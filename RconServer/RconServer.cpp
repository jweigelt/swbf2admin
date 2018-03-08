#include "RconServer.h"

RconServer::RconServer(uint16_t port, uint16_t maxClients)
{
	this->port = port;
	this->maxClients = maxClients;

	bf2server_init();
	bf2server_set_details(1);
}

RconServer::~RconServer()
{
}

void RconServer::Start()
{
	WSADATA wsaData;
	int err = NO_ERROR;

	if ((err = WSAStartup(MAKEWORD(2, 2), &wsaData)) != NO_ERROR) {
		Logger.Log(LogLevel_ERROR, "WSAStartup failed with error: %ld", err);
		return;
	}

	if ((listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET) {
		Logger.Log(LogLevel_ERROR, "socket failed with error: %ld", WSAGetLastError());
		WSACleanup();
		return;
	}

	sockaddr_in service;
	service.sin_family = AF_INET;
	service.sin_addr.s_addr = INADDR_ANY;
	service.sin_port = htons(port);

	if ((err = ::bind(listenSocket, (SOCKADDR *)&service, sizeof(service))) == SOCKET_ERROR) {
		Logger.Log(LogLevel_ERROR, "bind failed with error %u", WSAGetLastError());
		closesocket(listenSocket);
		WSACleanup();
		return;
	}

	if (listen(listenSocket, maxClients) == SOCKET_ERROR) {
		Logger.Log(LogLevel_ERROR, "listen failed with error %u", WSAGetLastError());
		closesocket(listenSocket);
		WSACleanup();
		return;
	}
	running = true;
	workThread = new thread(&RconServer::Listen, this);
	bf2server_set_chat_cb(std::bind(&RconServer::OnChatInput, this, std::placeholders::_1));
}

void RconServer::Stop()
{
	bf2server_set_chat_cb(NULL);
	running = false;
	closesocket(listenSocket);
	workThread->join();
	delete workThread;
}

void RconServer::Listen()
{
	Logger.Log(LogLevel_INFO, "Listening...");

	while (running) {
		SOCKET clientSocket;
		if ((clientSocket = accept(listenSocket, NULL, NULL)) == INVALID_SOCKET) {
			Logger.Log(LogLevel_WARNING, "Client connect failed with %ld", WSAGetLastError());
		}
		else {
			RconClient* client = new RconClient(clientSocket, std::bind(&RconServer::OnClientDisconnect, this, std::placeholders::_1));
			{
				lock_guard<mutex> lg(mtx);
				clients.push_back(client);
			}
			Logger.Log(LogLevel_INFO, "Client connected. %zu clients connected.", clients.size());
			client->Start();
		}
	}

	lock_guard<mutex> lg(mtx);
	for (RconClient* c : clients)
	{
		c->Stop();
		delete c;
	}

	clients.clear();
	if (running) {
		closesocket(listenSocket);
		running = false;
	}

	WSACleanup();
}

void RconServer::OnClientDisconnect(RconClient * client)
{
	lock_guard<mutex> lg(mtx);
	size_t idx = 0;
	for (size_t i = 0; i < clients.size(); i++) {
		if (clients.at(i) == client) {
			idx = i;
			break;
		}
	}
	clients.erase(clients.begin() + idx);
	delete client;
}

void RconServer::OnChatInput(string const & msg)
{
	lock_guard<mutex> lg(mtx);
	for (RconClient *c : clients) {
		c->OnChatInput(msg);
	}
}