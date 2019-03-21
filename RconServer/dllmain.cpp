#pragma once
#include "config.h"
#include "RconServer.h"
#include "bf2server.h"
#include <Windows.h>

static RconServer* dllmain_server;
static bool dllmain_running;
HANDLE dllmain_hThread;

DWORD WINAPI Run(LPVOID p) {
	dllmain_server = new RconServer(MAX_CONNECTIONS);
	dllmain_server->Start();

#ifdef _DEBUG
	Logger.SetMinLevelFile(LogLevel_VERBOSE);
#else
	Logger.SetMinLevelFile(LogLevel_ERROR);
#endif

	MapStatus prevStatus = MAP_IDLE;
	MapStatus newStatus = MAP_IDLE;
	while (dllmain_running) {
		newStatus = bf2server_get_map_status();
		if (newStatus != prevStatus && newStatus != MAP_IDLE) {
			dllmain_server->ReportEndgame();
		}
		prevStatus = newStatus;
		Sleep(10);
#ifdef _DEBUG
		if (GetAsyncKeyState(VK_ESCAPE) && GetAsyncKeyState(VK_BACK)) dllmain_running = false;
#endif
	}

	dllmain_server->Stop();
	delete dllmain_server;

	FreeLibraryAndExitThread((HMODULE)p, 0);
	return 0;
}

BOOL WINAPI DllMain(HINSTANCE hModule, DWORD dwReason, IN LPVOID dwReserved) {
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		//bf2server_init is the first thing we do so we can apply crash patches before server crashes
		bf2server_init();
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