#pragma once

#include <mutex>

enum LogLevel
{
	LogLevel_VERBOSE = 0,
	LogLevel_INFO = 1,
	LogLevel_WARNING = 2,
	LogLevel_ERROR = 3
};

class LoggerImpl
{
public:
	void log(LogLevel, const char *, ...);
	void SetMinLevelFile(LogLevel);

private:
	LogLevel minLevelStdout = LogLevel_WARNING;
	LogLevel minLevelFile = LogLevel_WARNING;
	std::mutex mtx;
};

extern LoggerImpl Logger;
