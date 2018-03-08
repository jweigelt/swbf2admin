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
	workThread->join();
	delete workThread;
}

void RconClient::Start()
{
	workThread = new std::thread(&RconClient::HandleConnection, this);
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
	bf2server_login();
	return (res == 1);
}

void RconClient::HandleCommand(std::string const & command)
{
	string res = bf2server_command(MESSAGETYPE_COMMAND, SENDER_REMOTE, bf2server_s2ws(command).c_str(), OUTPUT_BUFFER);
	vector<string> rows = vector<string>();
	size_t op = 0;
	size_t np = 0;

	while ((np = res.find('\n', np)) != string::npos) {
		string r = res.substr(op, np - op);
		rows.push_back(r);
		op = ++np;
	}
	Send(rows);
}

void RconClient::Send(vector<string> &response)
{
	lock_guard<mutex> lg(mtx);

	unsigned char rowLen = 0;
	unsigned char rows = (unsigned char)response.size();
	send(socket, (char*)&rows, 1, 0);

	for (string row : response) {
		rowLen = (unsigned char)row.length() + 1;
		send(socket, (char*)&rowLen, 1, 0);
		send(socket, row.c_str(), rowLen, 0);
	}
}

void RconClient::HandleConnection()
{
	unsigned char rows, sz, bytesRead, fragment;
	char* buffer;
	bool err = false;

	if (!(connected = CheckLogin())) {
		Logger.Log(LogLevel_VERBOSE, "Client login failed.");
	}

	while (connected) {
		bytesRead = 0;
		if (recv(socket, (char*)&rows, 1, 0) != 1) break;
		if (recv(socket, (char*)&sz, 1, 0) != 1) break;

		buffer = new char[sz];
	
		while (sz > bytesRead) {
			if ((fragment = recv(socket, buffer + bytesRead, sz - bytesRead, 0)) == SOCKET_ERROR) {
				err = true;
				break;
			}
			buffer[sz - 1] = 0;
			bytesRead += (char)fragment;
		}

		if (err) break;

		Logger.Log(LogLevel_VERBOSE, "Received command: %s", buffer);
		HandleCommand(string(buffer));
		delete[] buffer;
	}

	Logger.Log(LogLevel_VERBOSE, "Closing connection.");

	if (connected) {
		closesocket(socket);
		connected = false;
	}
	onDisconnect(this);
}