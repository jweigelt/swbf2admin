//
// Created by jan on 8/16/18.
//

#pragma once

#include <stdarg.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <iostream>
#include <fstream>
#include <memory>
#include <mutex>
enum LogLevel {
	LogLevel_VERBOSE = 0,
	LogLevel_INFO = 1,
	LogLevel_WARNING = 2,
	LogLevel_ERROR = 3,
	LOGLEVEL_CRITICAL = 4
};

static const char* LOG_LEVELS[] =
{
		"DEBUG | ",
		"INFO  | ",
		"WARN  | ",
		"ERROR | ",
		"CRASH | "
};

class _Logger
{
public:
	void log(LogLevel, const char*, ...);
	void SetMinLevelStdout(LogLevel);
	void SetMinLevelFile(LogLevel);
	void SetFileName(const std::string&);

private:
	void LogToFile(const char* s);
	LogLevel minLevelStdout = LogLevel_WARNING;
	LogLevel minLevelFile = LogLevel_INFO;
    std::string logFile = "./rconserver_log.txt";
    std::mutex mtx;
};

static _Logger Logger;