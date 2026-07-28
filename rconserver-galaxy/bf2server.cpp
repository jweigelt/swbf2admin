#include "bf2server.h"

#include <utility>

DWORD_PTR moduleBase;

//chat
DWORD_PTR chatCCAddr;

//mapfix
DWORD tickAddr, mapfixRetnAddr;
BYTE mapfixTicks;

FLOAT spawnValue;
DWORD_PTR spawnValueAddr;

std::function<void(std::string const &msg)> chatCB;

void bf2server_init() {
	Logger.log(LogLevel_VERBOSE, "Patching BattlefrontII process...");

	moduleBase = (DWORD)GetModuleHandleA("BattlefrontII.exe");
	mapfixRetnAddr = OFFSET_MAPFIX_RETN + moduleBase;
	tickAddr = (DWORD)&mapfixTicks;

	bf2server_patch_norender();
	bf2server_patch_password();
	bf2server_patch_votekick_exploit();
	bf2server_patch_maphang();
	bf2server_patch_dedicated();
	bf2server_patch_distance_lag();
	bf2server_patch_object_budget();
	bf2server_patch_netupdate();
	bf2server_patch_waitlate_grace();

	chatCCAddr = reinterpret_cast<DWORD>(&bf2server_chat_cc);
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

	spawnValueAddr = reinterpret_cast<DWORD>(&spawnValue);

	bf2server_patch_spawnvalue();
	bf2server_patch_pregame_spawn();

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

void bf2server_patch_distance_lag()
{
	BYTE playerMovesPatch[] = {
		//0x05 -> 0x20
		0x20
	};

	bf2server_patch_asm(0x001d38b8, (void*)playerMovesPatch, sizeof(playerMovesPatch));
}

void bf2server_patch_waitlate_grace()
{
	// /waitlate grace in turns (0..127); stock is 3.
	// nowaitlate continues to use zero grace.
	BYTE grace = 1;

	// 0x5BAACD: MOV [EBP-0x18],3; patch imm8 at 0x5BAAD0.
	bf2server_patch_asm(0x005BAAD0 - 0x400000, (void*)&grace, sizeof(grace));
}

void bf2server_patch_object_budget()
{
	// Object-state budget scale (0..1024); stock is 800.
	// At the default netUpdateSize, this is the byte threshold.
	DWORD objectBudget = 500;

	// 0x5CE71C: IMUL EAX,[netUpdateSize],800; patch imm32 at 0x5CE722.
	bf2server_patch_asm(0x005CE722 - 0x400000,
		(void*)&objectBudget, sizeof(objectBudget));
}

void bf2server_patch_asm(DWORD_PTR offset, void * patch, size_t patchSize)
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
	return std::string((char*)(addr));
}

void bf2server_set_chat_cc() {
	auto addr = reinterpret_cast<DWORD>(&chatCCAddr);
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

std::wstring bf2server_s2ws(std::string const & s)
{
	int len;
	int slength = (int)s.length() + 1;
	len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
	auto* buf = new wchar_t[len];
	MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
	std::wstring r(buf);
	delete[] buf;
	return r;
}

void bf2server_set_chat_cb(std::function<void(std::string const&msg)> onChat)
{
	chatCB = std::move(onChat);
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

// Pregame exit wrapper for the VanishAllPlayer call at 0x5C4F37.
// Runs the original cleanup, then resets the live spawn timers.
void __cdecl bf2server_pregame_spawn_cc()
{
	auto vanishAllPlayers = reinterpret_cast<void(__cdecl*)()>(
		moduleBase + OFFSET_VANISH_ALL_PLAYERS);
	vanishAllPlayers();

	auto spawnManager = *reinterpret_cast<BYTE**>(moduleBase + OFFSET_SPAWN_MANAGER);
	if (spawnManager == nullptr)
	{
		return;
	}

	// +0x5C is the cycle delay; +0x9C/+0xDC are the live cycle/slot timers.
	// Resetting both blocks cycle stamps that became eligible during pregame.
	for (DWORD team = 0; team < 8; ++team)
	{
		DWORD teamOffset = team * sizeof(FLOAT);
		FLOAT delay = *reinterpret_cast<FLOAT*>(spawnManager + 0x5C + teamOffset);
		*reinterpret_cast<FLOAT*>(spawnManager + 0x9C + teamOffset) = delay;
		*reinterpret_cast<FLOAT*>(spawnManager + 0xDC + teamOffset) = delay;
	}
}

void bf2server_patch_pregame_spawn()
{
	// 0x5C4F35: JLE -> JL so pregame ends at elapsed == max.
	// This removes the extra second where the displayed timer is zero.
	BYTE endAtZero = 0x7C;
	bf2server_patch_asm(OFFSET_PREGAME_END_BRANCH,
		(void*)&endAtZero, sizeof(endAtZero));

	// 0x5C4F37: replace CALL VanishAllPlayer with the transition wrapper.
	// The following PUSH 0 / SetPreGame(false) sequence is unchanged.
	BYTE callPatch[] = { 0xE8, 0x00, 0x00, 0x00, 0x00 };
	DWORD callAddress = static_cast<DWORD>(moduleBase + OFFSET_PREGAME_VANISH_CALL);
	*(DWORD*)&callPatch[1] = reinterpret_cast<DWORD>(&bf2server_pregame_spawn_cc)
		- (callAddress + sizeof(callPatch));
	bf2server_patch_asm(OFFSET_PREGAME_VANISH_CALL,
		(void*)callPatch, sizeof(callPatch));
}


// 30 UPS send scheduling detours; each preserves the game's EBP frame.

static DWORD g_curDstAddr;        // &_curDst (dest client index)         VA 0x01FA9C2C
static DWORD g_wo_resume;         // WriteObjects resume after MOV         VA 0x005CE58C
static DWORD g_su2_resume;        // SendUpdate2 resume after IMUL         VA 0x005D2E99
static DWORD g_su2_mark_resume;   // SendUpdate2 mark-slot resume          VA 0x005D2DFB
static DWORD g_su2_skip_resume;   // SendUpdate2 skip-slot resume          VA 0x005D2E21
static DWORD g_getTimeFn;         // time function                         VA 0x005B3840
static BYTE  g_pendCreate[256];   // per-client create emitted flag
static DWORD g_pendCreateAddr;    // &g_pendCreate[0]

// 0x5CE582: save WriteObjects [EBP-0x11] for _curDst.
// Overwrites MOV [EBP-0x84],6; resumes at 0x5CE58C.
// SendUpdate2 uses the saved flag to select its next-send delay.
void __declspec(naked) bf2_pendcreate_cc()
{
    __asm {
        pushad
        mov   eax, dword ptr [g_curDstAddr]
        mov   eax, dword ptr [eax]          // _curDst (dest client index)
        movzx ecx, byte ptr [ebp-11h]       // local_11: emitted a CREATE this send?
        mov   edx, dword ptr [g_pendCreateAddr]
        mov   byte ptr [edx+eax], cl        // g_pendCreate[_curDst] = local_11
        popad
        mov   dword ptr [ebp-84h], 6        // redo overwritten MOV [EBP-0x84],0x6
        mov   eax, dword ptr [g_wo_resume]
        jmp   eax                           // resume 0x5CE58C
    }
}

// 0x5D2DF1: skip the SendUpdate2 slot write when [EBP-0x4] == 2.
// Overwrites CALL 0x5B3840 + 2 bytes of MOVSS; resumes at 0x5D2DFB or 0x5D2E21.
// Slot index 2 means both tracked send-window slots are occupied.
void __declspec(naked) bf2_su2_slotfix_cc()
{
    __asm {
        cmp   dword ptr [ebp-4], 2          // local_4: free slot idx, or 2 if none free
        jb    su2_domark
        mov   eax, dword ptr [g_su2_skip_resume]   // no free slot -> skip marking (0x5D2E21)
        jmp   eax
    su2_domark:
        call  dword ptr [g_getTimeFn]       // redo CALL 0x5b3840 -> XMM0 = now (secs)
        movss dword ptr [ebp-14h], xmm0     // redo MOVSS [EBP-0x14], XMM0
        mov   eax, dword ptr [g_su2_mark_resume]
        jmp   eax                           // resume 0x5D2DFB (mark the free slot)
    }
}

// 0x5D2E8F: use [EBP-0x18] delay after a create, otherwise one turn.
// Overwrites ADD ECX,[EBP-0x18] + 4 bytes of IMUL; resumes at 0x5D2E99.
// g_pendCreate[client] selects the delay path.
void __declspec(naked) bf2_su2_delay_cc()
{
    __asm {
        mov   edx, dword ptr [ebp+8]        // client index (SendUpdate2 param)
        mov   eax, dword ptr [g_pendCreateAddr]
        movzx eax, byte ptr [eax+edx]       // g_pendCreate[idx]
        test  eax, eax
        jz    su2_fast
        add   ecx, dword ptr [ebp-18h]      // pending create -> real ping-adaptive delay
        jmp   su2_done
    su2_fast:
        add   ecx, 1                        // no pending create -> +1 (full 30 UPS)
    su2_done:
        imul  edx, dword ptr [ebp+8], 208h  // redo overwritten IMUL EDX,[EBP+8],0x208
        mov   eax, dword ptr [g_su2_resume]
        jmp   eax                           // resume 0x5D2E99 (MOV [EDX+0x1ECEF74],ECX)
    }
}

void bf2server_patch_send_scheduling()
{
    g_curDstAddr      = (DWORD)(moduleBase + 0x1BA9C2C); // _curDst          0x01FA9C2C
    g_wo_resume       = (DWORD)(moduleBase + 0x1CE58C);  // WO resume        0x005CE58C
    g_su2_resume      = (DWORD)(moduleBase + 0x1D2E99);  // SU2 resume       0x005D2E99
    g_su2_mark_resume = (DWORD)(moduleBase + 0x1D2DFB);  // SU2 do-mark      0x005D2DFB
    g_su2_skip_resume = (DWORD)(moduleBase + 0x1D2E21);  // SU2 skip-mark    0x005D2E21
    g_getTimeFn       = (DWORD)(moduleBase + 0x1B3840);  // get-now fn       0x005B3840
    g_pendCreateAddr  = (DWORD)&g_pendCreate[0];

    // 0x5D2DF1: install SendUpdate2 slot guard.
    // Full windows send without writing an out-of-range timestamp slot.
    BYTE d[] = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xE0 };
    *(DWORD*)&d[1] = (DWORD)&bf2_su2_slotfix_cc;
    bf2server_patch_asm(0x005D2DF1 - 0x400000, d, sizeof(d));

    // 0x5CE582: install WriteObjects create flag hook.
    // The flag is stored per destination for SendUpdate2 pacing.
    BYTE a[] = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xE0 };
    *(DWORD*)&a[1] = (DWORD)&bf2_pendcreate_cc;
    bf2server_patch_asm(0x005CE582 - 0x400000, a, sizeof(a));

    // 0x5D2E8F: install conditional SendUpdate2 pacing.
    // Creates use the stock delay; other updates use one turn.
    BYTE b[] = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xE0 };
    *(DWORD*)&b[1] = (DWORD)&bf2_su2_delay_cc;
    bf2server_patch_asm(0x005D2E8F - 0x400000, b, sizeof(b));
}

void bf2server_patch_netupdate()
{
    // 0x5338FA: NOP the GameLoop::Update render/present call.
    // Simulation and network update calls remain active.
    BYTE render_patch[] = {
        //call 0x6be110 (E8 11 A8 18 00) -> nop x5
        0x90, 0x90, 0x90, 0x90, 0x90
    };
    bf2server_patch_asm(0x005338FA - 0x400000,
        reinterpret_cast<void*>(render_patch), sizeof(render_patch));

    // 0x618B03: Sleep(10) -> Sleep(0) in the inactive-window loop.
    // The active-window path is unchanged.
    BYTE window_sleep_patch[] = {
        //push 0x0A -> push 0x00
        0x6A, 0x00
    };
    bf2server_patch_asm(0x00618B03 - 0x400000,
        reinterpret_cast<void*>(window_sleep_patch), sizeof(window_sleep_patch));

    // 0x5C9C19: remove the netCurMaxPlayers/2 send budget.
    // The per-client nextUpdateTurn gate remains active.
    BYTE send_all_patch[] = {
        //cdq; sub eax,edx; sar eax,1  = the "/2" (99 2B C2 D1 F8) -> nop x5
        0x90, 0x90, 0x90, 0x90, 0x90
    };
    bf2server_patch_asm(0x005C9C19 - 0x400000,
        reinterpret_cast<void*>(send_all_patch), sizeof(send_all_patch));

    // 0x5C9D56: JNZ -> JMP to bypass IsSendWindowOpen.
    // The SendUpdate2 slot guard handles full tracking windows.
    BYTE send_window_patch[] = { 0xEB };   // 0x75 -> 0xEB
    bf2server_patch_asm(0x005C9D56 - 0x400000,
        reinterpret_cast<void*>(send_window_patch), sizeof(send_window_patch));

    // Install create pacing and the SendUpdate2 slot guard.
    // Confirmed clients remain on the one-turn update cadence.
    bf2server_patch_send_scheduling();

    // 0x5C9D40: NOP the IsPipeFull skip.
    // Ticket and nextUpdateTurn gates remain active.
    BYTE pipe_full_off_patch[] = {
        //skip-if-IsPipeFull jmp (E9 53 FF FF FF) -> nop x5
        0x90, 0x90, 0x90, 0x90, 0x90
    };
    bf2server_patch_asm(0x005C9D40 - 0x400000,
        reinterpret_cast<void*>(pipe_full_off_patch), sizeof(pipe_full_off_patch));

}

int bf2server_lua_dostring(std::string const & code)
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
