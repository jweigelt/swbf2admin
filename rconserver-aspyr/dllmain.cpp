#include "config.h"
#include "Logger.h"
#include "RconServer.h"
#include "bf2server.h"
#include <Windows.h>
#include <atomic>

static std::atomic_bool dllmain_running = false;
HANDLE dllmain_hThread;

DWORD WINAPI Run(LPVOID p)
{
#ifdef _DEBUG
	Logger.SetMinLevelFile(LogLevel_VERBOSE);
#else
	Logger.SetMinLevelFile(LogLevel_WARNING);
#endif
	Logger.log(LogLevel_VERBOSE, "DLL loaded...");
	while (dllmain_running && GetModuleHandleW(L"Battlefront2.dll") == nullptr)
	{
		Sleep(10);
	}
	if (!dllmain_running) return 0;

	bf2server_init();
	RconServer server(MAX_CONNECTIONS);
	bool rconStarted = server.start();

	MapStatus prevStatus = MAP_IDLE;
	MapStatus newStatus = MAP_IDLE;
	unsigned int pendingEndgames = 0;
	while (dllmain_running)
	{
		newStatus = bf2server_get_map_status();
		if (newStatus != prevStatus && newStatus != MAP_IDLE)
		{
			++pendingEndgames;
		}
		prevStatus = newStatus;
		if (bf2server_pump_chat())
		{
			while (pendingEndgames > 0)
			{
				Logger.log(LogLevel_VERBOSE, "Detected endgame");
				if (rconStarted) server.reportEndgame();
				--pendingEndgames;
			}
		}
		bf2server_mapfix_tick();
		Sleep(50);
#ifdef _DEBUG
		if (GetAsyncKeyState(VK_ESCAPE) && GetAsyncKeyState(VK_BACK)) dllmain_running = false;
#endif
	}

	if (rconStarted) server.stop();

	FreeLibraryAndExitThread((HMODULE)p, 0);
}

BOOL WINAPI DllMain(HINSTANCE hModule, DWORD dwReason, IN LPVOID)
{
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
		// Keep patching, Winsock, and file I/O outside the Windows loader lock.
		DisableThreadLibraryCalls(hModule);
		dllmain_running = true;
		dllmain_hThread = CreateThread(0, 0, Run, hModule, 0, 0);
		break;

	case DLL_PROCESS_DETACH:
		dllmain_running = false;
		if (dllmain_hThread) WaitForSingleObject(dllmain_hThread, 1000);
		break;
	}

	return TRUE;
}
