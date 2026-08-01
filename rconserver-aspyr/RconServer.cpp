#include "RconServer.h"
#include "Logger.h"
#include "bf2server.h"

#include <algorithm>
#include <system_error>

RconServer::RconServer(uint16_t maxClients)
	: listenSocket(INVALID_SOCKET), port(static_cast<uint16_t>(bf2server_get_gameport())), maxClients(maxClients)
{
}

RconServer::~RconServer()
{
	if (running) stop();
}

bool RconServer::start()
{
	WSADATA wsaData{};
	int err = NO_ERROR;

	if ((err = WSAStartup(MAKEWORD(2, 2), &wsaData)) != NO_ERROR)
	{
		Logger.log(LogLevel_ERROR, "WSAStartup failed with error: %ld", err);
		return false;
	}

	if ((listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) == INVALID_SOCKET)
	{
		Logger.log(LogLevel_ERROR, "socket failed with error: %ld", WSAGetLastError());
		WSACleanup();
		return false;
	}

	sockaddr_in service{};
	service.sin_family = AF_INET;
	service.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
	service.sin_port = htons(port);

	if ((err = ::bind(listenSocket, (SOCKADDR *)&service, sizeof(service))) == SOCKET_ERROR)
	{
		Logger.log(LogLevel_ERROR, "bind failed with error %u", WSAGetLastError());
		closesocket(listenSocket);
		WSACleanup();
		return false;
	}

	if (::listen(listenSocket, maxClients) == SOCKET_ERROR)
	{
		Logger.log(LogLevel_ERROR, "listen failed with error %u", WSAGetLastError());
		closesocket(listenSocket);
		WSACleanup();
		return false;
	}
	running = true;
	try
	{
		workThread = std::thread(&RconServer::listen, this);
	}
	catch (const std::system_error &error)
	{
		running = false;
		closesocket(listenSocket);
		listenSocket = INVALID_SOCKET;
		WSACleanup();
		Logger.log(LogLevel_ERROR, "Unable to start RCON listener thread: %s", error.what());
		return false;
	}
	bf2server_set_chat_cb([this](const std::string &message) { onChatInput(message); });
	return true;
}

void RconServer::stop()
{
	bf2server_set_chat_cb(nullptr);
	running = false;
	closesocket(listenSocket);
	if (workThread.joinable()) workThread.join();

	std::vector<std::unique_ptr<RconClient>> activeClients;
	{
		std::lock_guard<std::mutex> lock(mtx);
		activeClients.swap(clients);
	}
	for (auto &client : activeClients) client->stop();
	activeClients.clear();
	WSACleanup();
}

void RconServer::listen()
{
	Logger.log(LogLevel_INFO, "Listening...");

	while (running)
	{
		SOCKET clientSocket;
		if ((clientSocket = accept(listenSocket, nullptr, nullptr)) == INVALID_SOCKET)
		{
			if (!running) break;
			Logger.log(LogLevel_WARNING, "Client connect failed with %ld", WSAGetLastError());
			Sleep(50);
			continue;
		}

		const DWORD sendTimeout = 1000;
		if (setsockopt(clientSocket, SOL_SOCKET, SO_SNDTIMEO, reinterpret_cast<const char *>(&sendTimeout),
					   static_cast<int>(sizeof(sendTimeout))) == SOCKET_ERROR)
		{
			Logger.log(LogLevel_WARNING, "Unable to set RCON send timeout: %d", WSAGetLastError());
			closesocket(clientSocket);
			continue;
		}

		size_t clientCount;
		{
			std::lock_guard<std::mutex> lock(mtx);
			removeFinishedClientsLocked();
			if (clients.size() >= maxClients)
			{
				closesocket(clientSocket);
				continue;
			}

			auto newClient = std::make_unique<RconClient>(clientSocket);
			clients.push_back(std::move(newClient));
			if (!clients.back()->start())
			{
				closesocket(clientSocket);
				clients.pop_back();
				continue;
			}
			clientCount = clients.size();
		}

		Logger.log(LogLevel_VERBOSE, "Client connected. %zu clients connected.", clientCount);
	}
}

void RconServer::removeFinishedClientsLocked()
{
	clients.erase(std::remove_if(clients.begin(), clients.end(),
								 [](const std::unique_ptr<RconClient> &client) { return client->isFinished(); }),
				  clients.end());
}

void RconServer::onChatInput(std::string const &msg)
{
	std::lock_guard<std::mutex> lock(mtx);
	for (auto &client : clients)
	{
		client->onChatInput(msg);
	}
}

void RconServer::reportEndgame()
{
	std::lock_guard<std::mutex> lock(mtx);
	for (auto &client : clients)
	{
		client->reportEndgame();
	}
}
