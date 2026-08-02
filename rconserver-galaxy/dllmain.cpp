#include "RconServer.h"
#include "Logger.h"
#include "bf2server.h"
#include "direct_transport/direct_transport_policy.h"
#include "direct_transport/direct_transport_server.h"
#include <Windows.h>
#include <atomic>
#include <cstdio>
#include <cstdint>

namespace
{
constexpr UINT kDirectTransportStartupExitCode = 0xD1;
constexpr std::uint16_t kMaxRconConnections = 8;
static std::atomic_bool dllmain_running = false;
} // namespace

DWORD WINAPI Run(LPVOID module)
{
	bool processExitRequested = false;

#ifdef _DEBUG
	Logger.SetMinLevelFile(LogLevel_VERBOSE);
#else
	Logger.SetMinLevelFile(LogLevel_WARNING);
#endif
	const bool patchesApplied = bf2server_init();
	if (patchesApplied)
		Logger.log(LogLevel_INFO, "RconServer_32 loaded; patches applied; spawn timer %gs.",
				   bf2server_get_spawnvalue());
	else
	{
		Logger.log(LogLevel_ERROR, "RconServer_32 patch installation failed.");
		return 0;
	}

	const auto directPolicy = bf2direct::ReadPolicy();
	bool directTransportStarted = false;
	if (!directPolicy.configured)
	{
		Logger.log(LogLevel_WARNING, "Direct transport disabled: BF2_DIRECT_POLICY is missing");
	}
	else if (!directPolicy.valid)
	{
		Logger.log(LogLevel_WARNING, "Direct transport disabled: BF2_DIRECT_POLICY is invalid");
	}
	else if (directPolicy.value == bf2direct::Policy::Disabled)
	{
		Logger.log(LogLevel_INFO, "Direct transport disabled");
	}
	else
	{
		std::string directError;
		if (!bf2direct::GetServerTransport().Initialize(directPolicy.value, directError))
		{
			Logger.log(LogLevel_ERROR, "Direct transport startup failed: %s", directError.c_str());
			TerminateProcess(GetCurrentProcess(), kDirectTransportStartupExitCode);
			return kDirectTransportStartupExitCode;
		}
		directTransportStarted = true;
		if (!directError.empty()) Logger.log(LogLevel_WARNING, "%s", directError.c_str());
	}

	RconServer server(kMaxRconConnections);
	bool rconStarted = server.start();

	MapStatus prevStatus = MAP_IDLE;
	bool endgamePending = false;
	while (dllmain_running)
	{
		const MapStatus newStatus = bf2server_get_map_status();
		if (prevStatus == MAP_IDLE && newStatus != MAP_IDLE) endgamePending = true;
		prevStatus = newStatus;
		if (bf2server_pump_chat() && endgamePending)
		{
			Logger.log(LogLevel_VERBOSE, "Detected endgame");
			if (rconStarted) server.reportEndgame();
			endgamePending = false;
		}
		bf2server_mapfix_tick();
		if (directTransportStarted) bf2direct::GetServerTransport().Tick();
		Sleep(50);
#if defined(_DEBUG)
		const bool escapeDown = (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;
		const bool backspaceDown = (GetAsyncKeyState(VK_BACK) & 0x8000) != 0;
		if (escapeDown && backspaceDown)
		{
			processExitRequested = true;
			dllmain_running = false;
		}
#endif
	}

	if (rconStarted) server.stop();
	if (directTransportStarted)
	{
		std::string directError;
		if (!bf2direct::GetServerTransport().Shutdown(directError))
		{
			Logger.log(LogLevel_ERROR, "Direct transport shutdown failed: %s", directError.c_str());
			TerminateProcess(GetCurrentProcess(), kDirectTransportStartupExitCode);
			return kDirectTransportStartupExitCode;
		}
	}
	if (processExitRequested)
	{
		Logger.log(LogLevel_INFO, "Debug shutdown complete; exiting process.");
		std::fflush(nullptr);
		ExitProcess(0);
	}

	FreeLibraryAndExitThread(static_cast<HMODULE>(module), 0);
}

BOOL WINAPI DllMain(HINSTANCE hModule, DWORD dwReason, IN LPVOID)
{
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
	{
		DisableThreadLibraryCalls(hModule);
		dllmain_running = true;
		HANDLE worker = CreateThread(0, 0, Run, hModule, 0, 0);
		if (worker == nullptr)
		{
			dllmain_running = false;
			return FALSE;
		}
		CloseHandle(worker);
		break;
	}

	case DLL_PROCESS_DETACH:
		dllmain_running = false;
		break;
	}

	return TRUE;
}
