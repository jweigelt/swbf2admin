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
	// /waitlate grace = how many turns the host waits for a late client move before
	// force-advancing (NetHostAdvanceTurns @ 0x005BAACD: MOV [EBP-0x18], 3). ONLY affects
	// waitlate (nowaitlate forces grace 0), and does NOT re-enable the rollback replay /
	// fast-timer (gated on netWaitLate==0, untouched). Lower = tighter hit-reg / snappier;
	// higher = wait longer for the correct move. Range 0..127.
	BYTE grace = 1;   // <-- compile-time tweak: 0/1 = tighter hit-reg, 3 = stock waitlate

	//imm32 low byte of MOV [EBP-0x18], 3 @ 0x005BAACD: 03 -> grace
	bf2server_patch_asm(0x005BAAD0 - 0x400000, (void*)&grace, sizeof(grace));
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


// Sending each client an update every tick (needed for 30 UPS) breaks two things:
//  - RTT corruption (root bug): on a full send window SendUpdate2 writes out of
//    bounds over the smoothed RTT (SRTT = now), so its next-send delay explodes and
//    starves the client. Stock avoids it by not sending on a full window; our
//    window-gate bypass removes that guard. Hook D skips the OOB write.
//  - Dropped create-confirms: a new object is re-sent every tick until the client
//    confirms it, but the client keeps only the newest packet per receive pass, so
//    the confirm is coalesced away and the object is rebuilt from a partial snapshot
//    with uninitialized fields. Hooks A+B pace an un-confirmed client at the normal
//    RTT-adaptive delay so the confirm survives; every tick once confirmed.
//
// Three code-cave detours (jmp preserves each function's EBP frame):
//   Hook D 0x5D2DF1 (SendUpdate2): skip the out-of-bounds window-slot write.
//   Hook A 0x5CE582 (WriteObjects): record whether this send emitted a create.
//   Hook B 0x5D2E8F (SendUpdate2): un-confirmed create -> RTT delay, else next tick.

static DWORD g_curDstAddr;        // &_curDst (dest client index)         VA 0x01FA9C2C
static DWORD g_wo_resume;          // WriteObjects resume (after MOV)      VA 0x005CE58C
static DWORD g_su2_resume;         // SendUpdate2 resume (after IMUL)      VA 0x005D2E99
static DWORD g_su2_mark_resume;    // SendUpdate2 do-mark resume           VA 0x005D2DFB
static DWORD g_su2_skip_resume;    // SendUpdate2 skip-mark resume         VA 0x005D2E21
static DWORD g_getTimeFn;          // "get now (float secs)" fn           VA 0x005B3840
static BYTE  g_pendCreate[256];   // per-client: last send emitted a create (zero-init;
                                  // 256 >> any player-index cap, so no overflow)
static DWORD g_pendCreateAddr;    // = &g_pendCreate[0]

// Hook A @ 0x5CE582. Overwrites MOV dword[EBP-0x84],6 (10 bytes; 7 used, resume
// 0x5CE58C). The create-list terminator's convergence point, so local_11 is final.
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

// Hook D @ 0x5D2DF1 (SendUpdate2, the RTT root fix). The slot-search loop exits with
// local_4=2 when both send-window slots are full (only reachable once the window gate
// is bypassed). Stock then marks a nonexistent "slot 2": MOVSS [P+2*4+0x1bc] =
// [P+0x1C4] = SRTT, writing `now` into the smoothed RTT -> RTT/delay/ping all run away
// (confirmed live via HW write-BP). Fix: if no slot is free, skip the marking - the
// send still goes out, just untracked this once, and SRTT stays real. Overwrites CALL
// 0x5b3840 (5) + 2 bytes of MOVSS[EBP-0x14] (7 total); do-mark redoes both, resumes
// 0x5D2DFB. EAX dead at loop exit.
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

// Hook B @ 0x5D2E8F. Overwrites ADD ECX,[EBP-0x18] (3 bytes) + 4 bytes of the following
// IMUL (7 total; resume 0x5D2E99). ECX = serverHostTurn. Un-confirmed create -> schedule
// next send at the real stock delay [EBP-0x18] (RTT-adaptive, correct only because Hook D
// keeps SRTT sane); else +1 tick (full 30 UPS). Replaces the send-every-tick byte patch.
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

    // Hook D @ 0x5D2DF1 (SendUpdate2): RTT root fix - skip the out-of-bounds window-slot
    // write on a full window, so the smoothed RTT is never overwritten with `now`.
    BYTE d[] = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xE0 };
    *(DWORD*)&d[1] = (DWORD)&bf2_su2_slotfix_cc;
    bf2server_patch_asm(0x005D2DF1 - 0x400000, d, sizeof(d));

    // Hook A @ 0x5CE582 (WriteObjects): record per-client "this send emitted a create".
    BYTE a[] = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xE0 };
    *(DWORD*)&a[1] = (DWORD)&bf2_pendcreate_cc;
    bf2server_patch_asm(0x005CE582 - 0x400000, a, sizeof(a));

    // Hook B @ 0x5D2E8F (SendUpdate2): conditional pacing; replaces the send-every-tick patch.
    BYTE b[] = { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xE0 };
    *(DWORD*)&b[1] = (DWORD)&bf2_su2_delay_cc;
    bf2server_patch_asm(0x005D2E8F - 0x400000, b, sizeof(b));
}

void bf2server_patch_netupdate()
{
    // GameLoop::Update: skip the per-tick render/present call -> CPU headroom to hold 30 UPS.
    BYTE render_patch[] = {
        //call 0x6be110 (E8 11 A8 18 00) -> nop x5
        0x90, 0x90, 0x90, 0x90, 0x90
    };
    bf2server_patch_asm(0x005338FA - 0x400000,
        reinterpret_cast<void*>(render_patch), sizeof(render_patch));

    // WindowCreate msg loop: Sleep(10) -> Sleep(0) on the inactive-window path, so
    // the OS doesn't throttle the tick loop when the server window loses focus.
    BYTE window_sleep_patch[] = {
        //push 0x0A -> push 0x00
        0x6A, 0x00
    };
    bf2server_patch_asm(0x00618B03 - 0x400000,
        reinterpret_cast<void*>(window_sleep_patch), sizeof(window_sleep_patch));

    // SendToClients: raise the per-call send budget from netCurMaxPlayers/2 to
    // netCurMaxPlayers so every player gets an update each call (the rate gate at
    // 0x5C9D16 keeps it to one send per client per turn).
    BYTE send_all_patch[] = {
        //cdq; sub eax,edx; sar eax,1  = the "/2" (99 2B C2 D1 F8) -> nop x5
        0x90, 0x90, 0x90, 0x90, 0x90
    };
    bf2server_patch_asm(0x005C9C19 - 0x400000,
        reinterpret_cast<void*>(send_all_patch), sizeof(send_all_patch));

    // SendToClients: bypass the ack-window gate (IsSendWindowOpen 0x5D3070) so a scheduled
    // send is never withheld waiting on the window (JNZ 0x75 -> JMP 0xEB @ 0x5C9D56). This
    // is what exposes the SendUpdate2 out-of-bounds write that Hook D fixes. Safe: the
    // next-send gate (0x5C9D0E) runs first and paces un-confirmed creates (see above).
    BYTE send_window_patch[] = { 0xEB };   // 0x75 -> 0xEB
    bf2server_patch_asm(0x005C9D56 - 0x400000,
        reinterpret_cast<void*>(send_window_patch), sizeof(send_window_patch));

    // Fix the SendUpdate2 RTT corruption (Hook D) and pace un-confirmed creates at the
    // stock delay (Hooks A/B); also replaces the send-every-tick byte patch above.
    bf2server_patch_send_scheduling();

    // SendToClients: remove the outbound bandwidth limiter (IsPipeFull skip @ 0x5C9D40).
    // Optional / no-op at scrim bandwidth (16 Mbit, ~10 players never fill the shared
    // leaky bucket); only matters on a constrained uplink.
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