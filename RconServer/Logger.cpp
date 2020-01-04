//
// Created by jan on 8/16/18.
//

#include "Logger.h"

void _Logger::log(LogLevel level, const char *msg, ...) {
	va_list args;

	if (minLevelStdout <= level) {
		printf("%s", LOG_LEVELS[level]);
		va_start(args, msg);
		vprintf(msg, args);
		va_end(args);
		printf("\n");
	}

	if (minLevelFile <= level) {
		va_start(args, msg);
		auto len = (size_t)vsnprintf(nullptr, 0, msg, args) + 1;
		va_end(args);

		auto buffer = make_unique<char[]>(len);
		va_start(args, msg);
		vsnprintf(buffer.get(), len, msg, args);
		va_end(args);
		LogToFile(buffer.get());
	}
}

void _Logger::SetMinLevelStdout(LogLevel level) {
	minLevelStdout = level;
}

void _Logger::SetMinLevelFile(LogLevel level) {
	minLevelFile = level;
}

void _Logger::SetFileName(const string &fileName) {
	logFile = fileName;
}

void _Logger::LogToFile(const char *s) {
	unique_lock<mutex> lg(mtx);
	ofstream f;
	f.open(logFile, std::ofstream::app);
	f << s << "\n";
	f.close();
}