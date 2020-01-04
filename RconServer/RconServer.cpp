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
	workThread = thread(&RconServer::listen, this);
	bf2server_set_chat_cb(std::bind(&RconServer::onChatInput, this, std::placeholders::_1));
}

void RconServer::stop()
{
	bf2server_set_chat_cb(NULL);
	running = false;
	closesocket(listenSocket);
	workThread.join();
}

void RconServer::listen()
{
	Logger.log(LogLevel_INFO, "Listening...");

	while (running) {
		SOCKET clientSocket;
		if ((clientSocket = accept(listenSocket, NULL, NULL)) == INVALID_SOCKET) {
			Logger.log(LogLevel_WARNING, "Client connect failed with %ld", WSAGetLastError());
		}
		else {
			auto client = make_shared<RconClient>(clientSocket, std::bind(&RconServer::onClientDisconnect, this, std::placeholders::_1));

			unique_lock<mutex> lg(mtx);
			clients.push_back(client);
			lg.unlock();

			Logger.log(LogLevel_INFO, "Client connected. %zu clients connected.", clients.size());
			client->start();
		}
	}

	unique_lock<mutex> lg(mtx);
	clients.clear();
	lg.unlock();

	if (running) {
		closesocket(listenSocket);
		running = false;
	}

	WSACleanup();
}

void RconServer::onClientDisconnect(RconClient * client)
{
	size_t idx = 0;
	unique_lock<mutex> lg(mtx);
	for (size_t i = 0; i < clients.size(); i++) {
		if (clients.at(i).get() == client) {
			idx = i;
			break;
		}
	}
	clients.erase(clients.begin() + idx);
	lg.unlock();
}

void RconServer::onChatInput(string const & msg)
{
	unique_lock<mutex> lg(mtx);
	for (auto &c : clients) {
		c->onChatInput(msg);
	}
}

void RconServer::reportEndgame() {
	unique_lock<mutex> lg(mtx);
	for (auto &c : clients) {
		c->reportEndgame();
	}
}