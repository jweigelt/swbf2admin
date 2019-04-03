#pragma once
#ifndef _M_IX86
#error Please compile the RconServer project for x86 architecture
#endif

#include <Windows.h>
#include "Logger.h"
#include "config.h"

using std::string;
using std::wstring;
using std::function;

/*
//LEGACY
#ifdef GALAXY
//GOG Galaxy Version
#define OFFSET_CHATINPUT 0x1B0030			//function pointer to chat input function
#define OFFSET_CHATSNPRINTF 0x1B2F67		//address to snprintf-call for chat-output
#define OFFSET_RESBUFFER 0x1BA2830			//address of command response buffer
#define OFFSET_ADMINPW 0x1A57E10			//address of admin-password
#define OFFSET_COMMAND_DETAILS 0x1A57E2C	//address of "details"-dword which will determine of command-output is verbose
#else
//Steam Version
#define OFFSET_CHATINPUT 0x1AFB00 + 0x1000
#define OFFSET_CHATSNPRINTF 0x1B2A47 + 0x1000
#define OFFSET_RESBUFFER 0x1BA3950 + 0x1000
#define OFFSET_ADMINPW 0x1A653A0
#define OFFSET_COMMAND_DETAILS 0x1A58EFB + 0x1000
#endif
*/

#ifdef GALAXY
//GoG Version
#define OFFSET_NORENDER_FIX 0x006BB37F - 0x400000
#define OFFSET_VOTECRASH_FIX 0x005D2282 - 0x400000
#define OFFSET_VOTEKICK_FIX 0x00599B11 - 0x400000

#define OFFSET_PASSWORD_FIX 0x00599BC5 - 0x400000
#define OFFSET_PASSWORD_PLAIN 0x01E31C40 - 0x400000

#define OFFSET_NUMPLAYERS_MOD 0x005D7F31 - 0x400000
#define OFFSET_DEDICATED_FIX  0x005D800F - 0x400000

#define OFFSET_SPAWNVALUE_MOD_FLOAT 0x0058D605 - 0x400000 + 4

#define OFFSET_MAPFIX_DETOUR 0x005B6076 - 0x400000
#define OFFSET_MAPFIX_RETN 0x005B607D- 0x400000
#define OFFSET_MAP_STATUS 0x01EB1054 - 0x00400000
#define MAPFIX_IDLE_TIMEOUT 0x64

#define OFFSET_CHATINPUT 0x005B0030 - 0x00400000
#define OFFSET_CHATSNPRINTF 0x005B2F67 - 0x00400000 + 2
#define OFFSET_RESBUFFER 0x01FA39D0 - 0x00400000 
#define OFFSET_COMMAND_DETAILS 0x01E58EBC - 0x00400000
#define OFFSET_ADMINPW 0x01E64330 - 0x00400000 
#define OFFSET_LOGGED_IN 0x01F9C2E2 - 0x00400000 

#define OFFSET_GAMEPORT 0x3E9EF4
#define OFFSET_IDLE 0x01E58EBD - 0x400000;

#else
//Steam Version
#define OFFSET_CHATINPUT 0x005AF090 - 0x00401000 + 0x1000
#define OFFSET_CHATSNPRINTF 0x005B1FC7 - 0x00401000 + 0x1000 
#define OFFSET_RESBUFFER 0x01FA2518 - 0x00401000 + 0x1000
#define OFFSET_COMMAND_DETAILS 0x01E57A0C - 0x00401000 + 0x1000
#define OFFSET_ADMINPW 0x1A57A10
#define OFFSET_LOGGED_IN 0x01F9AE32 - 0x00401000 + 0x1000
#endif

#define MESSAGETYPE_CHAT 1
#define MESSAGETYPE_COMMAND 0

#define OUTPUT_CHAT 0
#define OUTPUT_BUFFER -1

#define SENDER_SELF 1
#define SENDER_REMOTE 0

enum MapStatus : BYTE {
	MAP_IDLE = 0x00,
	MAP_LOADING_ENDGAME = 0x06,
	MAP_LOADING_WIN = 0x02
};

/**
 *	Initializes server-access
 **/
void bf2server_init();

/**
*	Fixes the /norender arg which normally crashes with the gog/steam binaries
**/
void bf2server_patch_norender();

/**
*	Fixes the infamous ScriptCB_.... votekick exploit
**/
void bf2server_patch_votekick_exploit();

/**
*	Fixes lobby passwords which are broken in the gog/steam binaries
**/
void bf2server_patch_password();

/**
*	Sets servermode to dedicated, increments playercount by 1
**/
void bf2server_patch_dedicated();

/**
*	Installs map-hanging monitor codecave
**/
void bf2server_patch_maphang();

/**
*	Patches SetSpawnDelay() so it uses our own spawn value per default
**/
void bf2server_patch_spawnvalue();

/**
*	Installs an asm patch
*	@param offset instruction offset from module base
*	@param patch
*	@param patchSize
**/
void bf2server_patch_asm(DWORD offset, void* patch, size_t patchSize);

/**
*	Gets the current game (map) status
**/
MapStatus bf2server_get_map_status();

/**
* Checks whether the server is busy loading
**/
bool bf2server_idle();

/**
*	Checks mapfix status and resets if required
**/
void bf2server_mapfix_tick();

/**
 *	Calls swbf2's chat/command handling function.
 *	@param messageType 0 (command) or 1 (chat)
 *	@param sender 0 (remote) or 1 (selfhost) (using selfhost won't return result)
 *	@param message command or chat message
 *	@param responseOutput 0 (chat) or -1 (buffer)
 **/
string bf2server_command(DWORD messageType, DWORD sender, const wchar_t* message, DWORD responseOutput);

/**
 *	Attaches a codecave to swbf2's chat-output
 **/
void bf2server_set_chat_cc();

/**
 *	Called when new chat is received
 **/
int __cdecl bf2server_chat_cc(char* buf, size_t sz, const char* fmt, ...);

/**
 *	Gets the server's admin password
 **/
string bf2server_get_adminpwd();

/**
 *	Converts string to wstring
 **/
wstring bf2server_s2ws(string const & s);

/**
 *	Sets Chat-callback
 **/
void bf2server_set_chat_cb(function<void(string const &msg)> onChat);

/**
*	Gets the server gameport (set via /gameport)
**/
USHORT bf2server_get_gameport();