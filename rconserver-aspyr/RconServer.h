#pragma once
#include <WinSock2.h>
#include <atomic>
#include <cstdint>
#include <memory>
#include <mutex>
#include <thread>
#include <vector>

#include "RconClient.h"

class RconServer
{
public:
	RconServer(uint16_t maxClients);
	~RconServer();
	bool start();
	void stop();
	void reportEndgame();

private:
	std::atomic_bool running = false;
	std::mutex mtx;
	std::vector<std::unique_ptr<RconClient>> clients;
	SOCKET listenSocket;

	uint16_t port;
	uint16_t maxClients;
	void listen();
	void removeFinishedClientsLocked();
	void onChatInput(std::string const &msg);

	std::thread workThread;
};
