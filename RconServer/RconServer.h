#pragma once
#pragma comment (lib, "Ws2_32.lib")
#include <WS2tcpip.h>
#include <stdint.h>
#include <vector>
#include <mutex>
#include <thread>
#include <memory>
#include <algorithm>
#include <inttypes.h>
#include "RconClient.h"
#include "Logger.h"

using std::string;
using std::vector;
using std::thread;
using std::mutex;
using std::unique_lock;
using std::shared_ptr;
using std::make_shared;

class RconServer
{
public:
	RconServer(uint16_t maxClients);
	~RconServer();
	void start();
	void stop();
	void listen();
	void reportEndgame();

private:
	bool running = false;
	mutex mtx;
	vector<RconClient*> clients;
	SOCKET listenSocket;

	uint16_t port;
	uint16_t maxClients;
	void onClientDisconnect(RconClient *client);
	void onChatInput(string const & msg);

	shared_ptr<thread> workThread;
};