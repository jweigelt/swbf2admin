#pragma once
#include <WinSock2.h>
#include <atomic>
#include <mutex>
#include <string>
#include <thread>
#include <vector>

class RconClient
{
public:
	explicit RconClient(SOCKET socket);
	~RconClient();
	void stop();
	bool start();
	bool isFinished() const;
	void onChatInput(std::string const &msg);
	void reportEndgame();

private:
	SOCKET socket;
	bool checkLogin();
	std::atomic<bool> connected{false};
	std::atomic<bool> finished{false};
	std::mutex mtx;
	std::thread workThread;

	void handleCommand(std::string const &command);
	void send(const std::vector<std::string> &response);
	void handleConnection();
	bool dispatchInternal(std::string const &command, std::string &res);
};
