#include "Logger.h"

_Logger::_Logger()
{
}

_Logger::~_Logger()
{
}

void _Logger::log(LogLevel level, const char* msg, ...) {

	unique_lock <mutex> lg(mtx);

	if (minLevelStdout <= level) {
		printf(LOG_LEVELS[level]);
		va_list args;
		va_start(args, msg);
		vprintf(msg, args);
		va_end(args);
		printf("\n");
	}

	if (minLevelFile <= level) {
		//TODO: fix me
		logToFile(msg);
	}
}

void _Logger::setMinLevelStdout(LogLevel level)
{
	minLevelStdout = level;
}

void _Logger::setMinLevelFile(LogLevel level)
{
	minLevelFile = level;
}

void _Logger::SetFileName(string const & fileName)
{
	logFile = fileName;
}

void _Logger::logToFile(string const &  s)
{
	ofstream f;
	f.open(logFile, f.app);
	f << s.c_str() << "\n";
	f.close();
}