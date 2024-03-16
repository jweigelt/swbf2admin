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

#define COMMAND_LUA "/lua "
#define RETURN_EPARAM "invalid parameters\n"
#define RETURN_OK "ok\n"
#define RETURN_BUSY "busy\n"

class RconClient
{
public:
	RconClient(SOCKET &socket, std::function<void(RconClient *c)> disconnectCB);
	~RconClient();
	void stop();
	void start();
	void onChatInput(std::string const & msg);
	void reportEndgame();

private:
	SOCKET socket;
	bool checkLogin();
    std::atomic<bool> connected;
    std::function<void(RconClient *c)> disconnectCB;
    std::shared_ptr<std::thread> workThread;
    std::mutex mtx;

	void handleCommand(std::string const & command);
	void send(std::vector<std::string> &response);
	void handleConnection();
	bool dispatchInternal(std::string const &command, std::string &res);
};