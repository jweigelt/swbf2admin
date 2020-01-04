#pragma once
#include <WS2tcpip.h>
#include <vector>
#include <functional>
#include <thread>
#include <memory>
#include <mutex>
#include <atomic>

#include "Logger.h"
#include "bf2server.h"
#include "md5.h"

using std::string;
using std::vector;
using std::thread;
using std::mutex;
using std::unique_lock;
using std::make_unique;
using std::function;
using std::atomic;
using std::make_shared;
using std::shared_ptr;

class RconClient
{
public:
	RconClient(SOCKET &socket, function<void(RconClient *c)> disconnectCB);
	~RconClient();
	void stop();
	void start();
	void onChatInput(string const & msg);
	void reportEndgame();

private:
	SOCKET socket;
	bool checkLogin();
	atomic<bool> connected;
	function<void(RconClient *c)> disconnectCB;
	shared_ptr<thread> workThread;
	mutex mtx;

	void handleCommand(string const & command);
	void send(vector<string> &response);
	void handleConnection();
};