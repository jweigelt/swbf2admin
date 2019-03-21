#include "bf2server.h"

DWORD moduleBase, chatCCAddr;
function<void(string const &msg)> chatCB;

void bf2server_init() {
	moduleBase = (DWORD)GetModuleHandle(L"BattlefrontII.exe");
	bf2server_patch_norender();
	bf2server_patch_votekick_exploit();

	chatCCAddr = (DWORD)&bf2server_chat_cc;
	bf2server_set_chat_cc();
}

void bf2server_patch_norender()
{
	BYTE patch[] = {
		0x90, 0x90, 0x90,				//push [ebp+8]	-> nop	
		0x8B, 0xCE,						//mov ecx, esi
		0x90, 0x90, 0x90, 0x90, 0x90,	//call 0x6bb440 -> nop
		0x30, 0xc0,						//test al, al	-> xor al, al
		0x90, 0x90						//jnz 0x6BB3A6  -> nop
	};

	bf2server_patch_asm(OFFSET_NORENDER_FIX, (void*)patch, sizeof(patch));
}

void bf2server_patch_votekick_exploit()
{
	BYTE crashPatch[] = {
		0x32, 0xC9, 0x90	//mov cl, byte ptr[ebp]  -> xor cl, cl
	};

	BYTE kickPatch[] = {
		0x90, 0x90, 0x90, 0x90, 0x90	//call 0x005A22A0 -> nop
	};

	bf2server_patch_asm(OFFSET_VOTECRASH_FIX, (void*)crashPatch, sizeof(crashPatch));
	bf2server_patch_asm(OFFSET_VOTEKICK_FIX, (void*)kickPatch, sizeof(kickPatch));
}

void bf2server_patch_password()
{
	//TODO
	BYTE patch[] = {
		0x90, 0x90,						//push 0x01 -> nop
		0x90, 0x90, 0x90, 0x90, 0x90,	//push 0x80 -> nop
	};

}

void bf2server_patch_asm(DWORD offset, void * patch, size_t patchSize)
{
	DWORD op, np;
	DWORD addr = moduleBase + offset;
	VirtualProtect((void*)addr, patchSize, PAGE_EXECUTE_READWRITE, &op);
	memcpy((void*)addr, patch, patchSize);
	VirtualProtect((void*)addr, patchSize, op, &np);
}

std::string bf2server_command(DWORD messageType, DWORD sender, const wchar_t* message, DWORD responseOutput) {
	//NOTE: function might not be threadsafe
	DWORD adminAccessAddr = moduleBase + OFFSET_LOGGED_IN;
	DWORD outputDetailsAddr = moduleBase + OFFSET_COMMAND_DETAILS;
	DWORD addr = moduleBase + OFFSET_CHATINPUT;
	*(BYTE*)adminAccessAddr = 1;
	*(BYTE*)outputDetailsAddr = 1;
	__asm {
		push messageType
		push sender
		mov edx, message
		mov ecx, responseOutput
		call dword ptr[addr];
		add esp, 8
	}
	*(BYTE*)adminAccessAddr = 0;
	*(BYTE*)outputDetailsAddr = 0;
	addr = moduleBase + OFFSET_RESBUFFER;
	return string((char*)(addr));
}

void bf2server_set_chat_cc() {
	DWORD addr = (DWORD)&chatCCAddr;
	//replace function pointer to snprintf with our own
	bf2server_patch_asm(OFFSET_CHATSNPRINTF, (void*)&addr, sizeof(addr));
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

USHORT bf2server_get_gameport()
{
	DWORD addr = moduleBase + OFFSET_GAMEPORT;
	return 	*(USHORT*)addr;
}

MapStatus bf2server_get_map_status()
{
	DWORD addr = moduleBase + OFFSET_MAP_STATUS;
	return 	(MapStatus)*(BYTE*)addr;
}

bool bf2server_idle() {
	DWORD addr = moduleBase + OFFSET_IDLE;
	return 	(*(BYTE*)addr) == 1;
}