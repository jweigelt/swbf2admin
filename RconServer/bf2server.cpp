#include "bf2server.h"

#include <utility>

LPVOID moduleBase;

void bf2server_init() {
    Logger.log(LogLevel_INFO,"Patching Battlefront2.dll");
	while(!(moduleBase = (LPVOID)GetModuleHandleW(L"Battlefront2.dll"))) {
        Sleep(10);
    }
    bf2server_patch_netupdate();
}

void bf2server_patch_netupdate()
{
    // Configure IsSendWindowOpen to resend
    // faster on dropped packet
    BYTE send_window_patch[] = {
            //.text:000000018022B6A0 0F 86 A0 00 00 jbe loc_18022B746
            //.text:000000018022B6A0 0F 86 A0 00 00 nop
            0x90, 0x90,0x90, 0x90,0x90, 0x90
    };

    bf2server_patch_asm(0x000000018022B6A0-0x0000000180000000,
                        reinterpret_cast<void*>(send_window_patch),
                        sizeof(send_window_patch));

    // Remote per Client outbound bandwidth limiter
    BYTE pipe_full_patch[] = {
            //.text:00000001803818EE 76 03 jbe short bandwidth_max
            //.text:00000001803818EE 90 90 jbe nop
            0x90, 0x90
    };

    bf2server_patch_asm(0x00000001803818EE-0x0000000180000000,
                        reinterpret_cast<void*>(pipe_full_patch),
                        sizeof(pipe_full_patch));

    // set next update timestamp in SentUpdate so we
    // can immediately tx in next server tick
    BYTE send_update_patch[] = {
            //.text:0000000180238EEB 03 C8 add ecx, eax
            //.text:0000000180238EEB FF C1 inc ecx
            0xFF, 0xC1
    };
    bf2server_patch_asm(0x0000000180238EEB-0x0000000180000000,
                        reinterpret_cast<void*>(send_update_patch),
                        sizeof(send_update_patch));

    BYTE cur_players_patch[] = {
            //.text:00000001802380F8 8B 05 4A 5D BB 00     mov eax, cs:netCurMaxPlayers
            //.text:00000001802380FE 99                    cdq
            //.text:00000001802380F8 b8 00 00 00 00 90     mov eax, 0x40
            //.text:00000001802380FE nop
            0xb8, 0x40, 0x00, 0x00, 0x00, 0x90, 0x90
    };
    *(DWORD*)&cur_players_patch[1] = 1024;
    bf2server_patch_asm(0x00000001802380F8-0x0000000180000000,
                        reinterpret_cast<void*>(cur_players_patch),
                        sizeof(cur_players_patch));
}

void bf2server_patch_asm(DWORD_PTR offset, LPVOID patch, size_t patchSize)
{
    DWORD op, np;
    auto addr = (LPVOID)((LPBYTE)moduleBase + offset);
    VirtualProtect(addr, patchSize, PAGE_EXECUTE_READWRITE, &op);
    memcpy(addr, patch, patchSize);
    VirtualProtect(addr, patchSize, op, &np);
}