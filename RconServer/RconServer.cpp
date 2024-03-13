#include "RconServer.h"

RconServer::RconServer(uint16_t maxClients)
{
	this->maxClients = maxClients;
	this->port = (uint16_t)bf2server_get_gameport();
}

RconServer::~RconServer()
{
	if (running) stop();
}

void RconServer::start()
{
	WSADATA wsaData;
	int err = NO_ERROR;

	if ((err = WSAStartup(MAKEWORD(2, 2), &wsaData)) != NO_ERROR) {
		Logger.log(LogLevel_ERROR, "WSAStartup failed with error: %ld", err);
		return;
	}

	if ((listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET) {
		Logger.log(LogLevel_ERROR, "socket failed with error: %ld", WSAGetLastError());
		WSACleanup();
		return;
	}

	sockaddr_in service;
	service.sin_family = AF_INET;
	service.sin_addr.s_addr = INADDR_ANY;
	service.sin_port = htons(port);

	if ((err = ::bind(listenSocket, (SOCKADDR *)&service, sizeof(service))) == SOCKET_ERROR) {
		Logger.log(LogLevel_ERROR, "bind failed with error %u", WSAGetLastError());
		closesocket(listenSocket);
		WSACleanup();
		return;
	}

	if (::listen(listenSocket, maxClients) == SOCKET_ERROR) {
		Logger.log(LogLevel_ERROR, "listen failed with error %u", WSAGetLastError());
		closesocket(listenSocket);
		WSACleanup();
		return;
	}
	running = true;
	workThread = std::make_shared<std::thread>(&RconServer::listen, this);
	bf2server_set_chat_cb(std::bind(&RconServer::onChatInput, this, std::placeholders::_1));
}

void RconServer::stop()
{
	bf2server_set_chat_cb(nullptr);
	running = false;
	closesocket(listenSocket);
	workThread->join();
}

void RconServer::listen()
{
	Logger.log(LogLevel_INFO, "Listening...");

	while (running) {
		SOCKET clientSocket;
		if ((clientSocket = accept(listenSocket, nullptr, nullptr)) == INVALID_SOCKET) {
			Logger.log(LogLevel_WARNING, "Client connect failed with %ld", WSAGetLastError());
		}
		else {
			auto client = new RconClient(clientSocket, std::bind(&RconServer::onClientDisconnect, this, std::placeholders::_1));

            std::unique_lock<std::mutex> lg(mtx);
			clients.push_back(client);
			lg.unlock();

			Logger.log(LogLevel_INFO, "Client connected. %zu clients connected.", clients.size());
			client->start();
		}
	}

    std::unique_lock<std::mutex> lg(mtx);
	for(auto c : clients) {
		c->stop();
	}
	lg.unlock();

	if (running) {
		closesocket(listenSocket);
		running = false;
	}

	WSACleanup();
}

void RconServer::onClientDisconnect(RconClient * client)
{
    std::unique_lock<std::mutex> lg(mtx);
	clients.erase(std::remove(clients.begin(), clients.end(), client), clients.end());
	client->stop();
	delete client;
	Logger.log(LogLevel_INFO, "Client removed. %zu clients connected.", clients.size());
}

void RconServer::onChatInput(std::string const & msg)
{
    std::unique_lock<std::mutex> lg(mtx);
	for (auto &c : clients) {
		c->onChatInput(msg);
	}
}

void RconServer::reportEndgame() {
    std::unique_lock<std::mutex> lg(mtx);
	for (auto &c : clients) {
		c->reportEndgame();
	}
}