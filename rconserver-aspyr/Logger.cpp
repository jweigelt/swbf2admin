#include "Logger.h"

#include <cstdarg>
#include <cstdio>
#include <fstream>

namespace
{
constexpr const char *kLogFile = "./rconserver_log.txt";
constexpr const char *kLogLevels[] = {"DEBUG | ", "INFO  | ", "WARN  | ", "ERROR | "};
} // namespace

LoggerImpl Logger;

void LoggerImpl::log(LogLevel level, const char *format, ...)
{
	if (minLevelStdout > level && minLevelFile > level) return;

	char message[4096] = {};
	va_list args;
	va_start(args, format);
	vsnprintf(message, sizeof(message), format, args);
	va_end(args);
	message[sizeof(message) - 1] = '\0';

	std::lock_guard<std::mutex> lock(mtx);
	if (minLevelStdout <= level)
	{
		printf("%s%s\n", kLogLevels[level], message);
	}

	if (minLevelFile <= level)
	{
		std::ofstream file(kLogFile, std::ofstream::app);
		file << kLogLevels[level] << message << '\n';
	}
}

void LoggerImpl::SetMinLevelFile(LogLevel level)
{
	minLevelFile = level;
}
