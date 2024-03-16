#pragma once
#include "bf2server.h"
static bool dllmain_running;
HANDLE dllmain_hThread;

BOOL WINAPI DllMain(HINSTANCE hModule, DWORD dwReason, IN LPVOID dwReserved) {
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		bf2server_init();
		dllmain_running = true;
		//dllmain_hThread = CreateThread(0, 0, Run, hModule, 0, 0);
		break;

	case DLL_PROCESS_DETACH:
		dllmain_running = false;
		//WaitForSingleObject(dllmain_hThread, 1000);
		break;
	}

	return TRUE;
}