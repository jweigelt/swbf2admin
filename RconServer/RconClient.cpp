#include "RconClient.h"

RconClient::RconClient(SOCKET & socket, std::function<void(RconClient*c)> onDisconnect)
{
	this->socket = socket;
	this->onDisconnect = onDisconnect;
}

RconClient::~RconClient() { }

void RconClient::Stop()
{
	connected = false;
	closesocket(socket);
	workThread.join();
}

void RconClient::Start()
{
	workThread = thread(&RconClient::HandleConnection, this);
}

void RconClient::OnChatInput(std::string const & msg)
{
	std::vector<std::string> rows = std::vector<std::string>();
	rows.push_back(msg);
	Send(rows);
}

bool RconClient::CheckLogin()
{
	char pwd[33], magic, res;
	pwd[32] = 0x00;

	if (recv(socket, pwd, 32, 0) != 32) return false;
	if (recv(socket, &magic, 1, 0) != 1) return false;
	if (magic != 0x64) return false;

	string pwdHash = md5(bf2server_get_adminpwd());

	if (pwdHash.compare(pwd) == 0) {
		Logger.Log(LogLevel_VERBOSE, "Client logged in.", pwd);
		res = 1;
	}
	else {
		Logger.Log(LogLevel_VERBOSE, "Client sent wrong password '%s'", pwd);
		res = 0;
	}

	send(socket, &res, 1, 0);
	return (res == 1);
}

void RconClient::HandleCommand(std::string const & command)
{
	string res;
	if (bf2server_idle()) {
		res = bf2server_command(MESSAGETYPE_COMMAND, SENDER_REMOTE, bf2server_s2ws(command).c_str(), OUTPUT_BUFFER);
	}
	else {
		res = "busy\n";
	}

	vector<string> rows = vector<string>();
	size_t op = 0;
	size_t np = 0;

	while ((np = res.find('\n', np)) != string::npos) {
		string r = res.substr(op, np - op);
		rows.emplace_back(r);
		op = ++np;
	}
	Send(rows);
}

void RconClient::Send(vector<string> &response)
{
	unsigned char rowLen = 0;
	unsigned char rows = (unsigned char)response.size();

	{
		unique_lock<mutex> lg(mtx);
		send(socket, (char*)&rows, 1, 0);

		for (string row : response) {
			rowLen = (unsigned char)row.length() + 1;
			send(socket, (char*)&rowLen, 1, 0);
			send(socket, row.c_str(), rowLen, 0);
		}
	}
}

void RconClient::HandleConnection()
{
	unsigned char rows, sz, bytesRead, fragment;
	bool err = false;

	if (!(connected = CheckLogin())) {
		Logger.Log(LogLevel_VERBOSE, "Client login failed.");
	}

	while (connected) {
		bytesRead = 0;
		if (recv(socket, (char*)&rows, 1, 0) != 1) break;
		if (recv(socket, (char*)&sz, 1, 0) != 1) break;

		auto buffer = make_unique<char[]>(sz);

		while (sz > bytesRead) {
			if ((fragment = recv(socket, buffer.get() + bytesRead, sz - bytesRead, 0)) == SOCKET_ERROR) {
				err = true;
				break;
			}
			buffer.get()[sz - 1] = 0;
			bytesRead += (char)fragment;
		}

		if (!err) {
			Logger.Log(LogLevel_VERBOSE, "Received command: %s", buffer.get());
			HandleCommand(string(buffer.get()));
		}
	}

	Logger.Log(LogLevel_VERBOSE, "Closing connection.");

	if (connected) {
		closesocket(socket);
		connected = false;
	}
	onDisconnect(this);
}

void RconClient::ReportEndgame() {
	auto v = vector<string>();
	v.emplace_back("Game has ended");
	Send(v);
}