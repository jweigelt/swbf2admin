#pragma once
#ifndef _M_IX86
#error Please compile the RconServer project for x86 architecture
#endif

#include <functional>
#include <string>
#include <Windows.h>

// GOG BattlefrontII.exe offsets.

#define OFFSET_NORENDER_FIX	 0x006BB37F - 0x400000
#define OFFSET_VOTECRASH_FIX 0x005D2282 - 0x400000
#define OFFSET_VOTEKICK_FIX	 0x00599B11 - 0x400000

#define OFFSET_PASSWORD_FIX	  0x00599BC5 - 0x400000
#define OFFSET_PASSWORD_PLAIN 0x01E31C40 - 0x400000

#define OFFSET_NUMPLAYERS_MOD 0x005D7F31 - 0x400000
#define OFFSET_DEDICATED_FIX  0x005D800F - 0x400000

#define OFFSET_SPAWNVALUE_MOD_FLOAT 0x0058D605 - 0x400000 + 4
#define OFFSET_PREGAME_END_BRANCH	0x005C4F35 - 0x400000
#define OFFSET_PREGAME_VANISH_CALL	0x005C4F37 - 0x400000
#define OFFSET_VANISH_ALL_PLAYERS	0x005C50D0 - 0x400000
#define OFFSET_SPAWN_MANAGER		0x01EB0FE8 - 0x400000
#define OFFSET_IS_PLAYING			0x005C4530 - 0x400000
#define OFFSET_NET_CUR_MAX_PLAYERS	0x01E64408 - 0x400000
#define OFFSET_FIND_CHARACTER		0x00429430 - 0x400000

#define OFFSET_MAPFIX_DETOUR 0x005B6076 - 0x400000
#define OFFSET_MAPFIX_RETN	 0x005B607D - 0x400000
#define OFFSET_MAP_STATUS	 0x01EB1054 - 0x00400000
#define MAPFIX_IDLE_TIMEOUT	 0x64

#define OFFSET_CHATINPUT	   0x005B0030 - 0x00400000
#define OFFSET_CHATSNPRINTF	   0x005B2F67 - 0x00400000 + 2
#define OFFSET_RESBUFFER	   0x01FA39D0 - 0x00400000
#define OFFSET_COMMAND_DETAILS 0x01E58EBC - 0x00400000
#define OFFSET_ADMINPW		   0x01E64330 - 0x00400000
#define OFFSET_LOGGED_IN	   0x01F9C2E2 - 0x00400000

#define OFFSET_GAMEPORT	  0x3E9EF4
#define OFFSET_IDLE		  0x01E58EBD - 0x400000
#define OFFSET_TEAM_ARRAY 0x007EAAA0 - 0x400000

#define OFFSET_UPS_CLIENT_LIMITER 0x005C9C19 - 0x400000

#define OFFSET_LUA_STATE	   0x01E58E50 - 0x400000
#define OFFSET_LUA_LOAD_BUFFER 0x0069C0C0 - 0x400000
#define OFFSET_LUA_PCALL	   0x0069CF40 - 0x400000
#define OFFSET_LUA_SET_TOP	   0x0069D490 - 0x400000

#define LUA_OK 0

#define MESSAGETYPE_COMMAND 0

#define OUTPUT_BUFFER -1

#define SENDER_REMOTE 0

enum MapStatus : BYTE
{
	MAP_IDLE = 0x00,
	MAP_LOADING_ENDGAME = 0x06,
	MAP_LOADING_WIN = 0x02
};

/**
 *	Initializes server-access
 **/
bool bf2server_init();

/**
 *	Fixes the /norender arg which normally crashes with the gog/steam binaries
 **/
void bf2server_patch_norender();

/**
 *	Fixes lobby passwords which are broken in the gog/steam binaries
 **/
void bf2server_patch_password();

/**
 *	Sets servermode to dedicated, increments playercount by 1
 **/
void bf2server_patch_dedicated();

/**
 *	Fixes the infamous ScriptCB_.... votekick exploit
 **/
void bf2server_patch_votekick_exploit();

/**
 *	Installs map-hanging monitor codecave
 **/
void bf2server_patch_maphang();

/**
 *	Relays the nearest 32 player moves instead of 5.
 **/
void bf2server_patch_distance_lag();

/**
 *	Sets /waitlate grace to one host turn.
 **/
void bf2server_patch_waitlate_grace();

/**
 *	Prioritizes EntityMine state records and CreateOrdnance events.
 **/
void bf2server_patch_object_budget();

/**
 *	Removes dedicated-loop delays and enables per-turn client updates.
 **/
void bf2server_patch_netupdate();

/**
 *	Clamps dispenser throw strength before item creation.
 **/
void bf2server_patch_speedpacks();

/**
 *	Allows zero-cost jumps within the locked speed-boundary grace.
 **/
void bf2server_patch_locked_jump();

/**
 *	Ends sprint when a failed roll attempt leaves Energy exhausted.
 **/
void bf2server_patch_infinite_sprint();

/**
 *	Clears pending membership when a pre-play player disconnects.
 **/
void bf2server_patch_preplay_disconnect();

/**
 *	Reads the host spawn delay from SPAWN_TIMER.
 **/
void bf2server_patch_spawnvalue();

/**
 *	Resets spawn timers and required waves when pregame ends.
 **/
void bf2server_patch_pregame_spawn();

/**
 *	Calls swbf2's chat/command handling function.
 *	@param messageType 0 (command) or 1 (chat)
 *	@param sender 0 (remote) or 1 (selfhost) (using selfhost won't return result)
 *	@param message command or chat message
 *	@param responseOutput 0 (chat) or -1 (buffer)
 **/
std::string bf2server_command(DWORD messageType, DWORD sender, const wchar_t *message, DWORD responseOutput);

/**
 *	Installs the server chat-output hook.
 **/
void bf2server_set_chat_cc();

/**
 *	Called when new chat is received
 **/
int __cdecl bf2server_chat_cc(char *buf, size_t sz, const char *fmt, ...);

/**
 *	Gets the server's admin password
 **/
std::string bf2server_get_adminpwd();

/**
 *	Converts string to wstring
 **/
std::wstring bf2server_s2ws(std::string const &s);

/**
 *	Sets Chat-callback
 **/
void bf2server_set_chat_cb(std::function<void(std::string const &msg)> onChat);

/**
 *	Delivers queued chat and reports whether the queue was drained
 **/
bool bf2server_pump_chat();

/**
 *	Gets the server gameport (set via /gameport)
 **/
USHORT bf2server_get_gameport();

/**
 * Gets the configured respawn-wave delay
 **/
FLOAT bf2server_get_spawnvalue();

/**
 *	Gets the current game (map) status
 **/
MapStatus bf2server_get_map_status();

/**
 * Reads the native idle flag used to gate RCON commands.
 **/
bool bf2server_idle();

/**
 * Checks whether /status can read the team scores
 **/
bool bf2server_status_ready();

/**
 *	Resets the map-hang counter while map status is idle
 **/
void bf2server_mapfix_tick();

/**
 *	Executes lua code in the ingame context of the server
 **/
int bf2server_lua_dostring(std::string const &code);
