#pragma once
#pragma comment (lib, "Ws2_32.lib")
#include <WS2tcpip.h>
#include <stdint.h>
#include <vector>
#include <mutex>
#include <thread>

#include "RconClient.h"
#include "Logger.h"

using std::string;
using std::vector;
using std::thread;
using std::mutex;
using std::lock_guard;

class RconServer
{
public:
	RconServer(uint16_t port, uint16_t maxClients);
	~RconServer();
	void Start();
	void Stop();
	void Listen();

private:
	bool running = false;
	mutex mtx;
	vector<RconClient*> clients = vector<RconClient*>();
	SOCKET listenSocket;

	uint16_t port;
	uint16_t maxClients;
	void OnClientDisconnect(RconClient *client);
	void OnChatInput(string const & msg);

	std::thread* workThread;
};