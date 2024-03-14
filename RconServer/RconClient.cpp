#include "RconClient.h"

#include <utility>

RconClient::RconClient(SOCKET & socket, std::function<void(RconClient*c)> disconnectCB)
{
	this->socket = socket;
	this->disconnectCB = std::move(disconnectCB);
}

RconClient::~RconClient() { }

void RconClient::stop()
{
	connected = false;
}

void RconClient::start()
{
	workThread = std::make_shared<std::thread>(&RconClient::handleConnection, this);
	workThread->detach();
}

void RconClient::onChatInput(std::string const & msg)
{
	std::vector<std::string> rows = std::vector<std::string>();
	rows.push_back(msg);
	send(rows);
}

bool RconClient::checkLogin()
{
	char pwd[33], magic, res;
	pwd[32] = 0x00;

	if (recv(socket, pwd, 32, 0) != 32) return false;
	if (recv(socket, &magic, 1, 0) != 1) return false;
	if (magic != 0x64) return false;

    std::string pwdHash = md5(bf2server_get_adminpwd());

	if (pwdHash == pwd) {
		Logger.log(LogLevel_VERBOSE, "Client logged in.", pwd);
		res = 1;
	}
	else {
		Logger.log(LogLevel_VERBOSE, "Client sent wrong password '%s'", pwd);
		res = 0;
	}

	::send(socket, &res, 1, 0);
	return (res == 1);
}

void RconClient::handleCommand(std::string const & command)
{
    std::string res;
	if (bf2server_idle() && bf2server_get_map_status() == MAP_IDLE) {
		
		if(!dispatchInternal(command, res)) {
			res = bf2server_command(MESSAGETYPE_COMMAND, SENDER_REMOTE, bf2server_s2ws(command).c_str(), OUTPUT_BUFFER);
			Logger.log(LogLevel_VERBOSE, "Executed command '%s', result: '%s'", command.c_str(), res.c_str());
		}
	}
	else {
		Logger.log(LogLevel_VERBOSE, "Server is busy - telling the client...'");
		res = RETURN_BUSY;
	}

    auto rows = std::vector<std::string>();
	size_t op = 0;
	size_t np = 0;

	while ((np = res.find('\n', np)) != std::string::npos) {
        std::string r = res.substr(op, np - op);
		rows.emplace_back(r);
		op = ++np;
	}
	send(rows);
}

void RconClient::send(std::vector<std::string> &response)
{
	unsigned char rowLen = 0;
	auto rows = static_cast<unsigned char>(response.size());

	{
        std::unique_lock<std::mutex> lg(mtx);
		::send(socket, reinterpret_cast<char*>(&rows), 1, 0);

		for (std::string &row : response) {
			rowLen = static_cast<unsigned char>(row.length() + 1);
			::send(socket, reinterpret_cast<char*>(&rowLen), 1, 0);
			::send(socket, row.c_str(), rowLen, 0);
		}
	}
}

void RconClient::handleConnection()
{
	unsigned char rows, sz, bytesRead, fragment;
	bool err = false;

	if (!(connected = checkLogin())) {
		Logger.log(LogLevel_VERBOSE, "Client login failed.");
	}

	while (connected) {
		bytesRead = 0;
		if (recv(socket, (char*)&rows, 1, 0) != 1) break;
		if (recv(socket, (char*)&sz, 1, 0) != 1) break;

		auto buffer = std::make_unique<char[]>(sz);

		while (sz > bytesRead) {
			if ((fragment = recv(socket, buffer.get() + bytesRead, sz - bytesRead, 0)) == SOCKET_ERROR) {
				err = true;
				connected = false;
				break;
			}
			buffer.get()[sz - 1] = 0;
			bytesRead += static_cast<char>(fragment);
		}

		if (!err) {
			Logger.log(LogLevel_VERBOSE, "Received command: %s", buffer.get());
			handleCommand(std::string(buffer.get()));
		}
	}

	Logger.log(LogLevel_VERBOSE, "Closing connection.");
	closesocket(socket);
	disconnectCB(this);
}

bool RconClient::dispatchInternal(std::string const & command, std::string &res)
{
	if (command.rfind(COMMAND_LUA, 0) == 0) {
		auto ll = strlen(COMMAND_LUA);
		if(command.size() > ll) {
			auto lr = bf2server_lua_dostring(command.substr(ll));
			res = RETURN_OK;
		}
		else {
			res = RETURN_EPARAM;
		}
		return true;
	}
	return false;
}

void RconClient::reportEndgame() {
	auto v = std::vector<std::string>();
	v.emplace_back("Game has ended");
	send(v);
}