#pragma once
#include "config.h"
#include "RconServer.h"
#include "bf2server.h"
#include <Windows.h>

static RconServer* dllmain_server;
static bool dllmain_running;
HANDLE dllmain_hThread;

DWORD WINAPI Run(LPVOID p) {
	dllmain_server = new RconServer(RCON_PORT, MAX_CONNECTIONS);
	dllmain_server->Start();

#ifdef _DEBUG
	Logger.SetMinLevelFile(LogLevel_VERBOSE);
	while (!(GetAsyncKeyState(VK_ESCAPE) && GetAsyncKeyState(VK_BACK))) {
		Sleep(1000);
	}
#else
	Logger.SetMinLevelFile(LogLevel_ERROR);
	while (dllmain_running) Sleep(100);
#endif

	dllmain_server->Stop();
	delete dllmain_server;

	FreeLibraryAndExitThread((HMODULE)p, 0);
	return 0;
}

BOOL WINAPI DllMain(HINSTANCE hModule, DWORD dwReason, IN LPVOID dwReserved) {

	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		dllmain_running = true;
		dllmain_hThread = CreateThread(0, 0, Run, hModule, 0, 0);
		break;

	case DLL_PROCESS_DETACH:
		dllmain_running = false;
		WaitForSingleObject(dllmain_hThread, 1000);
		break;
	}

	return TRUE;
}