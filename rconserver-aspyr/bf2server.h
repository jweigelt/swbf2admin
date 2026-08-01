#pragma once
#ifndef _M_X64
#error Please compile the Classic Collection RconServer project for x64 architecture
#endif

#include <functional>
#include <string>
#include <Windows.h>

// Classic Collection Patch 3 Battlefront2.dll RVAs.

// RCON, chat, Lua, and launch/runtime globals.
#define OFFSET_CHATINPUT	   0x0025C7E0
#define OFFSET_PLAYER_NAME	   0x00271620
#define OFFSET_RESBUFFER	   0x009C1450
#define OFFSET_COMMAND_DETAILS 0x009C13FE
#define OFFSET_ADMINPW		   0x009C13A0
#define OFFSET_LOGGED_IN	   0x009C13FF
#define OFFSET_GAMEPORT		   0x0064B194
#define OFFSET_IDLE			   0x00642671
#define OFFSET_LUA_STATE	   0x009AFB38
#define OFFSET_LUA_EXECUTE	   0x003863A0

// Standalone patch sites.
#define OFFSET_VOTECRASH_FIX		 0x00279F51
#define OFFSET_VOTEKICK_FIX			 0x0023F0B6
#define OFFSET_MAPFIX_DETOUR		 0x00269120
#define OFFSET_MAP_STATUS			 0x00AEFF90
#define OFFSET_DISTANCE_LAG			 0x0028DA86
#define OFFSET_WAITLATE_GRACE		 0x003CE7DD
#define OFFSET_WEAPON_DISPENSER_FIRE 0x003681E0
#define OFFSET_OBJECT_BUDGET		 0x003CE750
#define OFFSET_SEND_NET_EVENTS		 0x00283690
#define OFFSET_LOCKED_JUMP			 0x00172210
#define OFFSET_SPRINT_ROLL_CALL		 0x0018323E
#define OFFSET_PREPLAY_DISCONNECT	 0x002924B0
#define OFFSET_SET_NOT_PLAYING		 0x00285E30
#define OFFSET_SPAWNVALUE_CALLBACK	 0x0022F000
#define OFFSET_PREGAME_END_BRANCH	 0x00288447
#define OFFSET_PREGAME_VANISH		 0x00288770
#define OFFSET_SPAWN_GATE_CALL		 0x000C0839
#define OFFSET_SPAWN_GATE_RELAY		 0x00271EE3
#define OFFSET_PLATFORM_LOBBY		 0x0064AA58
#define OFFSET_PLATFORM_STATE_A		 0x00F83B40
#define OFFSET_PLATFORM_STATE_B		 0x00F83B41

// Update scheduling patch sites.
#define OFFSET_UPS_CLIENT_LIMITER 0x00283E2E
#define OFFSET_SEND_UPDATE2_DELAY 0x00284C6B

// Network globals and per-player storage used by the ports.
#define OFFSET_NET_ENABLED			   0x00E765CC
#define OFFSET_STAGED_NET_ENABLED	   0x00E765CD
#define OFFSET_NET_UPDATE_SIZE		   0x006481A0
#define OFFSET_NET_CUR_MAX_PLAYERS	   0x00E765C0
#define OFFSET_HOST_TURN			   0x00E765B8
#define OFFSET_CURRENT_DESTINATION	   0x009DFC1C
#define OFFSET_CURRENT_PLAYERS		   0x009E04F0
#define OFFSET_PLAYING_MASK			   0x009E0228
#define OFFSET_ORDINARY_EVENT_HEAD	   0x009EA89C
#define OFFSET_ORDINARY_EVENT_RING	   0x009EAAD0
#define OFFSET_SPAWN_MANAGER		   0x00AD7A68
#define OFFSET_HOST_COMM_PROBLEMS	   0x009C51FD
#define OFFSET_UNBOUNDED_EVENTS		   0x009C51FE
#define OFFSET_ENTITY_MINE_RTTI		   0x00691A10
#define OFFSET_ENTITY_MINE_VTABLE	   0x0050FFF0
#define OFFSET_ENTITY_MINE_WRITE_VFUNC 0x005101E0

// Native helpers used by semantic replacements.
#define OFFSET_GET_TIME				 0x003CFC10
#define OFFSET_HOST_INDEX_PREDICATE	 0x00275CE0
#define OFFSET_MAP_DEADLINE			 0x009C51E4
#define OFFSET_MAP_EXPIRED			 0x009C51E3
#define OFFSET_MAP_DEADLINE_SENTINEL 0x00524BEC
#define OFFSET_HOST_DEADLINE_DELAY	 0x00505D30
#define OFFSET_CLIENT_DEADLINE_DELAY 0x00504510
#define OFFSET_LUA_TO_NUMBER		 0x003854A0
#define OFFSET_SET_SPAWN_DELAY		 0x00324F30
#define OFFSET_IS_PLAYING			 0x00276360
#define OFFSET_FIND_CHARACTER		 0x00093A00
#define OFFSET_JUMP_EPSILON			 0x0050448C
#define OFFSET_ROLL_USING_ENERGY	 0x0017A8E0
#define OFFSET_SCOPED_OBJECT_COUNT	 0x00271E10
#define OFFSET_SCORE_NET_EVENT		 0x00270D30
#define OFFSET_FIND_ORDNANCE_CLASS	 0x0026E870
#define OFFSET_EVENT_SECTION_LIMIT	 0x003CE630
#define OFFSET_PACKET_BYTE_COUNT	 0x00401160
#define OFFSET_WRITE_NET_EVENT		 0x0028B820
#define OFFSET_WRITE_PACKET_BIT		 0x00289410

// Return site and caller-frame layout for the sole ObjectBudget helper call.
#define OFFSET_WRITE_OBJECTS_BUDGET_RETURN 0x0028D396
#define WRITE_OBJECT_LIST_STACK_OFFSET	   0x2E0

#define MAPFIX_IDLE_TIMEOUT		 0x64
#define ORDINARY_EVENT_RING_MASK 0x1FF

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
 *	Selects Classic's Photon lobby from PLATFORM_LOBBY.
 **/
void bf2server_patch_platform_lobby();

/**
 *	Fixes the infamous ScriptCB_.... votekick exploit
 **/
void bf2server_patch_votekick_exploit();

/**
 *	Installs the map-hang monitor hook.
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
 *	Enables full client selection and native-windowed per-turn updates.
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
 *	Fixes disconnected players blocking the same-team spawn queue.
 **/
void bf2server_patch_spawnbug();

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
 * Classic validates /status objects in its native handler.
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
