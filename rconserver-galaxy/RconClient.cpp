#include "RconClient.h"

#include <utility>

namespace
{

bool receive_all(SOCKET socket, void *destination, size_t length)
{
	char *buffer = static_cast<char *>(destination);
	while (length > 0)
	{
		int received = recv(socket, buffer, static_cast<int>(length), 0);
		if (received <= 0) return false;
		buffer += received;
		length -= static_cast<size_t>(received);
	}
	return true;
}

bool send_all(SOCKET socket, const void *source, size_t length)
{
	const char *buffer = static_cast<const char *>(source);
	while (length > 0)
	{
		int sent = ::send(socket, buffer, static_cast<int>(length), 0);
		if (sent <= 0) return false;
		buffer += sent;
		length -= static_cast<size_t>(sent);
	}
	return true;
}

} // namespace

RconClient::RconClient(SOCKET &socket, std::function<void(RconClient *c)> disconnectCB)
{
	this->socket = socket;
	this->disconnectCB = std::move(disconnectCB);
}

RconClient::~RconClient() {}

void RconClient::stop()
{
	connected = false;
}

void RconClient::start()
{
	workThread = std::make_shared<std::thread>(&RconClient::handleConnection, this);
	workThread->detach();
}

void RconClient::onChatInput(std::string const &msg)
{
	std::vector<std::string> rows = std::vector<std::string>();
	rows.push_back(msg);
	send(rows);
}

bool RconClient::checkLogin()
{
	char pwd[33], magic, res;
	pwd[32] = 0x00;

	if (!receive_all(socket, pwd, 32)) return false;
	if (!receive_all(socket, &magic, 1)) return false;
	if (magic != 0x64) return false;

	std::string pwdHash = md5(bf2server_get_adminpwd());

	if (pwdHash == pwd)
	{
		Logger.log(LogLevel_VERBOSE, "Client logged in.", pwd);
		res = 1;
	}
	else
	{
		res = 0;
	}

	if (!send_all(socket, &res, 1)) return false;
	return (res == 1);
}

void RconClient::handleCommand(std::string const &command)
{
	std::string res;
	if (bf2server_idle() && bf2server_get_map_status() == MAP_IDLE &&
		(command != "/status" || bf2server_status_ready()))
	{

		if (!dispatchInternal(command, res))
		{
			res = bf2server_command(MESSAGETYPE_COMMAND, SENDER_REMOTE, bf2server_s2ws(command).c_str(),
									static_cast<DWORD>(OUTPUT_BUFFER));
			Logger.log(LogLevel_VERBOSE, "Executed command '%s', result: '%s'", command.c_str(), res.c_str());
		}
	}
	else
	{
		Logger.log(LogLevel_VERBOSE, "Server is busy - telling the client...'");
		res = RETURN_BUSY;
	}

	auto rows = std::vector<std::string>();
	size_t op = 0;
	size_t np;

	while (op < res.size())
	{
		np = res.find('\n', op);
		std::string r = res.substr(op, np - op);
		if (!r.empty() && r.back() == '\r') r.pop_back();
		rows.emplace_back(r);
		if (np == std::string::npos) break;
		op = np + 1;
	}
	send(rows);
}

void RconClient::send(std::vector<std::string> &response)
{
	unsigned char rowLen = 0;
	bool oversized = response.size() > 0xFF;
	for (const std::string &row : response)
	{
		if (row.size() >= 0xFF)
		{
			oversized = true;
			break;
		}
	}
	if (oversized)
	{
		Logger.log(LogLevel_WARNING, "RCON response exceeds the protocol limit.");
	}
	static const std::string limitError = "RCON response exceeds protocol limit";
	auto rows = static_cast<unsigned char>(oversized ? 1 : response.size());

	{
		std::unique_lock<std::mutex> lg(mtx);
		if (!send_all(socket, &rows, 1))
		{
			connected = false;
			shutdown(socket, SD_BOTH);
			return;
		}

		for (size_t index = 0; index < rows; ++index)
		{
			const std::string &row = oversized ? limitError : response[index];
			rowLen = static_cast<unsigned char>(row.length() + 1);
			if (!send_all(socket, &rowLen, 1) || !send_all(socket, row.c_str(), rowLen))
			{
				connected = false;
				shutdown(socket, SD_BOTH);
				return;
			}
		}
	}
}

void RconClient::handleConnection()
{
	unsigned char rows, sz;

	if (!(connected = checkLogin()))
	{
		Logger.log(LogLevel_VERBOSE, "Client login failed.");
	}

	while (connected)
	{
		if (!receive_all(socket, &rows, 1)) break;
		if (!receive_all(socket, &sz, 1) || sz == 0) break;

		auto buffer = std::make_unique<char[]>(sz);
		if (!receive_all(socket, buffer.get(), sz)) break;
		buffer.get()[sz - 1] = 0;

		Logger.log(LogLevel_VERBOSE, "Received command: %s", buffer.get());
		handleCommand(std::string(buffer.get()));
	}

	Logger.log(LogLevel_VERBOSE, "Closing connection.");
	closesocket(socket);
	disconnectCB(this);
}

bool RconClient::dispatchInternal(std::string const &command, std::string &res)
{
	if (command.rfind(COMMAND_LUA, 0) == 0)
	{
		auto ll = strlen(COMMAND_LUA);
		if (command.size() > ll)
		{
			bf2server_lua_dostring(command.substr(ll));
			res = RETURN_OK;
		}
		else
		{
			res = RETURN_EPARAM;
		}
		return true;
	}
	return false;
}

void RconClient::reportEndgame()
{
	auto v = std::vector<std::string>();
	v.emplace_back("Game has ended");
	send(v);
}
