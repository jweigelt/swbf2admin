#include "bf2server.h"

DWORD moduleBase;

//chat
DWORD chatCCAddr;

//mapfix
DWORD tickAddr, mapfixRetnAddr;
BYTE mapfixTicks;

FLOAT spawnValue;
DWORD spawnValueAddr;

FLOAT updateRate;
DWORD updateRateAddr;

DWORD netUpdateClientLimit = 1024;

function<void(string const &msg)> chatCB;

void bf2server_init() {
	Logger.log(LogLevel_VERBOSE, "Patching BattlefrontII process...");

	moduleBase = (DWORD)GetModuleHandle(L"BattlefrontII.exe");
	mapfixRetnAddr = OFFSET_MAPFIX_RETN + moduleBase;
	tickAddr = (DWORD)&mapfixTicks;

	bf2server_patch_norender();
	bf2server_patch_password();
	bf2server_patch_votekick_exploit();
	bf2server_patch_maphang();
	bf2server_patch_dedicated();

	updateRate = 0.001f;
	updateRateAddr = (DWORD)&updateRate;
#ifdef EXPERIMENTAL_UPS
	bf2server_patch_netupdate();
#endif

	chatCCAddr = (DWORD)&bf2server_chat_cc;
	bf2server_set_chat_cc();

	char* env_buffer = nullptr;
	size_t env_size;
	errno_t err = _dupenv_s(&env_buffer, &env_size, "SPAWN_TIMER");
	if (err || env_buffer == nullptr)
	{
		spawnValue = 15.0f;
	}
	else
	{
		spawnValue = static_cast<float>(atof(env_buffer));
	}
	free(env_buffer);

	spawnValueAddr = (DWORD)&spawnValue;
	bf2server_patch_spawnvalue();


	Logger.log(LogLevel_VERBOSE, "All patches applied.");
}


void bf2server_patch_norender()
{
	BYTE patch[] = {
		//push [ebp+8] -> nop
		0x90, 0x90, 0x90,

		//mov ecx, esi
		0x8B, 0xCE,

		//call 0x6bb440 -> nop
		0x90, 0x90, 0x90, 0x90, 0x90,

		//test al, al -> xor al, al
		0x30, 0xc0,

		//jnz 0x6BB3A6 -> nop
		0x90, 0x90
	};

	bf2server_patch_asm(OFFSET_NORENDER_FIX, (void*)patch, sizeof(patch));
}

void bf2server_patch_votekick_exploit()
{
	BYTE crashPatch[] = {
		//mov cl, byte ptr[ebp]
		//-> xor cl, cl
		//-> nop
		0x32, 0xC9,
		0x90
	};

	BYTE kickPatch[] = {
		//call 005A22A0 -> nop
		0x90, 0x90, 0x90, 0x90, 0x90
	};

	bf2server_patch_asm(OFFSET_VOTECRASH_FIX, (void*)crashPatch, sizeof(crashPatch));
	bf2server_patch_asm(OFFSET_VOTEKICK_FIX, (void*)kickPatch, sizeof(kickPatch));
}

void bf2server_patch_password()
{
	BYTE patch[] = {
		//push 0x01 -> nop
		0x90, 0x90,

		//push 0x80 -> nop
		0x90, 0x90, 0x90, 0x90, 0x90,

		//lea edx, [ebp-AC]
		//mov ecx, offset 007AA3F8
		//-> nop
		//-> lea eax, ds:1e31c40
		0x90, 0x90, 0x90, 0x90, 0x90,
		0x8D, 0x05, 0x40, 0x1c, 0xe3, 0x01,

		//call 005A2380 -> nop
		0x90, 0x90, 0x90, 0x90, 0x90,

		//lea eax, [ebp-AC] -> nop
		0x90, 0x90, 0x90, 0x90, 0x90, 0x90
	};

	*(DWORD*)&patch[14] = OFFSET_PASSWORD_PLAIN + moduleBase;

	bf2server_patch_asm(OFFSET_PASSWORD_FIX, (void*)patch, sizeof(patch));
}

void bf2server_patch_dedicated()
{
	BYTE numPlayersPatch[] = {
		//push[esp + 0D0h + var_B8]
		//-> push 1
		//-> inc edi
		//-> nop
		0x6A, 0x01,
		0x47,
		0x90
	};

	BYTE serverTypePatch[] = {
		//push 1
		//-> push 2
		0x6A, 0x02
	};

	bf2server_patch_asm(OFFSET_NUMPLAYERS_MOD, (void*)numPlayersPatch, sizeof(numPlayersPatch));
	bf2server_patch_asm(OFFSET_DEDICATED_FIX, (void*)serverTypePatch, sizeof(serverTypePatch));
}

void __declspec(naked) bf2server_mapfix_cc() {
	__asm {
		mov eax, dword ptr[tickAddr]
		inc byte ptr[eax]

		cmp byte ptr[eax], MAPFIX_IDLE_TIMEOUT
		jge loc_force

		loc_retn :
		mov ecx, dword ptr[mapfixRetnAddr]
			jmp ecx

			loc_force :
		xor eax, eax
			jmp loc_retn
	}
}

void bf2server_patch_maphang()
{
	BYTE detourPatch[] = {
		//movzx eax, offset 01E64359
		//-> mov eax, <ccAddr>
		//jmp eax
		0xB8, 0x00, 0x00, 0x00, 0x00,
		0xff, 0xe0
	};

	*(DWORD*)&detourPatch[1] = (DWORD)&bf2server_mapfix_cc;
	bf2server_patch_asm(OFFSET_MAPFIX_DETOUR, detourPatch, sizeof(detourPatch));
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
	bf2server_patch_asm(OFFSET_CHATSNPRINTF, (void*)&addr, sizeof(DWORD));
}

int __cdecl bf2server_chat_cc(char* buf, size_t sz, const char* fmt, ...) {
	int ret = -1;

	va_list args;
	va_start(args, fmt);
	ret = vsnprintf(buf, sz, fmt, args);
	va_end(args);
	Logger.log(LogLevel_VERBOSE, buf);
	if (chatCB != NULL) chatCB(std::string(buf));
	return ret;
}

std::string bf2server_get_adminpwd()
{
	DWORD addr = moduleBase + OFFSET_ADMINPW;
	return std::string((char*)addr);
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

void bf2server_mapfix_tick()
{
	if (bf2server_get_map_status() == MAP_IDLE && mapfixTicks > 0) {
		mapfixTicks = 0;
	}
}

void bf2server_patch_spawnvalue()
{
	bf2server_patch_asm(OFFSET_SPAWNVALUE_MOD_FLOAT, (void*)&spawnValueAddr, sizeof(DWORD));
}

void bf2server_patch_netupdate()
{
	BYTE ratePatch[] = {
		//movss xmm0, ds:float_0_1 ; <- 1/update rate
		0xF3, 0x0F, 0x10, 0x05, 0xD8, 0x2E, 0x7B, 0x00
	};

	BYTE clientLimitPatch[] = {
		//cdq
		//sub eax, edx
		//sar eax, 1
		//-> mov eax, 0x40
		0xb8, 0x40, 0x00, 0x00, 0x00
	};

	*(DWORD*)&ratePatch[4] = updateRateAddr;
	*(DWORD*)&clientLimitPatch[1] = netUpdateClientLimit;
	bf2server_patch_asm(OFFSET_UPS_RATE, ratePatch, sizeof(ratePatch));
	bf2server_patch_asm(OFFSET_UPS_CLIENT_LIMITER, clientLimitPatch, sizeof(clientLimitPatch));
	
	BYTE patch[] = {
		//jmp     short loop 
		//-> nop
		0x90, 0x90
	};


	bf2server_patch_asm(OFFSET_UPS_LIMITER, (void*)patch, sizeof(patch));
}

int bf2server_lua_dostring(string const & code)
{
	auto s = reinterpret_cast<DWORD>(code.c_str());
	auto l = static_cast<DWORD>(code.size());
	auto L = *(reinterpret_cast<DWORD*>(moduleBase + OFFSET_LUA_STATE));
	DWORD luaL_loadbuffer = moduleBase + OFFSET_LUA_LOAD_BUFFER;
	DWORD lua_pcall = moduleBase + OFFSET_LUA_PCALL;
	DWORD res;

	__asm {
		push 0
		push l
		push s
		push L
		call dword ptr [luaL_loadbuffer]
		add esp, 16
		mov res, eax
	}

	if (res == LUA_OK) {
		__asm {
			push 0
			push 0
			push 0
			push L
			call dword ptr [lua_pcall]
			add esp, 16
			mov res, eax
		}
		Logger.log(LogLevel_VERBOSE, "lua finished with result: %i", res);
	}
	else {
		Logger.log(LogLevel_VERBOSE, "lua parse failed: %i", res);
	}
}

int __cdecl bf2server_lua_invoke_event(lua_State *L)
{
	Logger.log(LogLevel_ERROR, "Hello from lua");
	return LUA_OK;
}

void bf2server_lua_register(const string & name, lua_CFunction fn)
{
	auto L = *(reinterpret_cast<DWORD*>(moduleBase + OFFSET_LUA_STATE));
	DWORD lua_pushcclosure = moduleBase + OFFSET_LUA_PUSHCCLOSURE;
	DWORD lua_setglobal = moduleBase + OFFSET_LUA_SETGLOBAL;
	auto n = name.c_str();
	DWORD res;

	__asm {
		push 0
		push fn
		push L
		call dword ptr[lua_pushcclosure]
		add esp, 12
	}

	__asm {
		push n
		push L
		call dword ptr[lua_setglobal]
		add esp, 8
	}
}