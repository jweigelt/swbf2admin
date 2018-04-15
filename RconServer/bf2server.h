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
#define OFFSET_CHATINPUT 0x005B0030 - 0x00401000 + 0x1000
#define OFFSET_CHATSNPRINTF 0x005B2F67 - 0x00401000 + 0x1000
#define OFFSET_RESBUFFER 0x01FA39D0 - 0x00401000 + 0x1000
#define OFFSET_COMMAND_DETAILS 0x01E58EBC - 0x00401000 + 0x1000
#define OFFSET_ADMINPW 0x01E64330 - 0x00401000 + 0x1000
#define OFFSET_LOGGED_IN 0x01F9C2E2 - 0x00401000 + 0x1000
#define OFFSET_GAMEPORT 0x3E9EF4
//1A64330 00400000
#else
//Steam Version
/*#define OFFSET_CHATINPUT 0x005AF090 - 0x00401000 + 0x1000
#define OFFSET_CHATSNPRINTF 0x005B1FC7 - 0x00401000 + 0x1000 
#define OFFSET_RESBUFFER 0x01FA2518 - 0x00401000 + 0x1000
#define OFFSET_COMMAND_DETAILS 0x01E57A0C - 0x00401000 + 0x1000
#define OFFSET_ADMINPW 0x1A57A10
#define OFFSET_LOGGED_IN 0x01F9AE32 - 0x00401000 + 0x1000
*/
#endif

#define ASM_NOP 0x90
#define ASM_JMP 0xE9
#define ASM_CALL 0xFF
#define ASM_DWORD_PTR 0x15

#define MESSAGETYPE_CHAT 1
#define MESSAGETYPE_COMMAND 0
#define DETAILS_VERBOSE 1
#define DETAILS_NORMAL 0

#define OUTPUT_CHAT 0
#define OUTPUT_BUFFER -1
#define SENDER_SELF 1
#define SENDER_REMOTE 0

/**
 *	Initializes server-access
 **/
void bf2server_init();

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
 *	Authenticate admin session
 **/
bool bf2server_login();

/**
 *	Enables / Disables detailed command out
 **/
void bf2server_set_details(BYTE mode);

/**
 *	Converts string to wstring
 **/
wstring bf2server_s2ws(string const & s);

/**
 *	Sets Chat-callback
 **/
void bf2server_set_chat_cb(function<void(string const &msg)> onChat);

USHORT bf2server_get_gameport();