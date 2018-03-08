#include "bf2server.h"

DWORD moduleBase, chatCCAddr;
function<void(string const &msg)> chatCB;

void bf2server_init() {
	moduleBase = (DWORD)GetModuleHandle(L"BattlefrontII.exe");
	chatCCAddr = (DWORD)&bf2server_chat_cc;
	bf2server_set_chat_cc();
}

std::string bf2server_command(DWORD messageType, DWORD sender, const wchar_t* message, DWORD responseOutput) {
	//NOTE: function might not be threadsafe
	DWORD aa = moduleBase + OFFSET_LOGGED_IN;
	DWORD addr = moduleBase + OFFSET_CHATINPUT;
	*(BYTE*)aa = 1;
	__asm {
		push messageType
		push sender
		mov edx, message
		mov ecx, responseOutput
		call dword ptr[addr];
		add esp, 8
	}
	*(BYTE*)aa = 0;
	addr = moduleBase + OFFSET_RESBUFFER;
	return string((char*)(addr));
}

void bf2server_set_chat_cc() {
	DWORD op, np;
	DWORD addr = moduleBase + OFFSET_CHATSNPRINTF;

	VirtualProtect((void*)addr, 6, PAGE_EXECUTE_READWRITE, &op);

	*(BYTE*)addr = ASM_CALL;
	*(BYTE*)(addr + 1) = ASM_DWORD_PTR;
	*(DWORD*)(addr + 2) = (DWORD)&chatCCAddr;

	VirtualProtect((void*)addr, 6, op, &np);
}

int __cdecl bf2server_chat_cc(char* buf, size_t sz, const char* fmt, ...) {
	int ret = -1;

	va_list args;
	va_start(args, fmt);
	ret = vsnprintf(buf, sz, fmt, args);
	va_end(args);
	Logger.Log(LogLevel_VERBOSE, buf);
	if (chatCB != NULL) chatCB(std::string(buf));
	return ret;
}

std::string bf2server_get_adminpwd()
{
	DWORD addr = moduleBase + OFFSET_ADMINPW;
	return std::string((char*)addr);
}

bool bf2server_login()
{
	//string loginCommand = string("/login ") + bf2server_get_adminpwd();
	//string res = bf2server_command(MESSAGETYPE_COMMAND, SENDER_SELF,  bf2server_s2ws(loginCommand).c_str(), OUTPUT_BUFFER);

	//return (res.compare("logged in\n") == 0);
	return true;
}

void bf2server_set_details(BYTE mode)
{
	DWORD addr = moduleBase + OFFSET_COMMAND_DETAILS;
	*(BYTE*)addr = mode;
}

wstring bf2server_s2ws(string const & s)
{
	int len;
	int slength = (int)s.length() + 1;
	len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
	wchar_t* buf = new wchar_t[len];
	MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
	std::wstring r(buf);
	delete[] buf;
	return r;
}

void bf2server_set_chat_cb(function<void(string const&msg)> onChat)
{
	chatCB = onChat;
}
