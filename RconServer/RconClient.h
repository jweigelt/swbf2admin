#pragma once
#include <WS2tcpip.h>
#include <vector>
#include <functional>
#include <thread>
#include <memory>
#include "Logger.h"
#include "bf2server.h"
#include "md5.h"

using std::string;
using std::vector;
using std::thread;
using std::mutex;
using std::unique_lock;
using std::make_unique;

class RconClient
{
public:
	RconClient(SOCKET &socket, function<void(RconClient *c)> onDisconnect);
	~RconClient();
	void Stop();
	void Start();
	void OnChatInput(string const & msg);
	void ReportEndgame();

private:
	SOCKET socket;
	bool CheckLogin();
	bool connected;
	function<void(RconClient *c)> onDisconnect = NULL;
	thread workThread;
	mutex mtx;

	void HandleCommand(string const & command);
	void Send(vector<string> &response);
	void HandleConnection();
};