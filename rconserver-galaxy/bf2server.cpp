#include "bf2server.h"

#include <cmath>
#include <cstdlib>
#include <cstring>
#include <deque>
#include <mutex>
#include <utility>

DWORD_PTR moduleBase;

// chat
DWORD_PTR chatCCAddr;

// mapfix
DWORD tickAddr, mapfixRetnAddr;
volatile LONG mapfixTicks;

FLOAT spawnValue = 15.0f;
DWORD_PTR spawnValueAddr;

std::function<void(std::string const &msg)> chatCB;
std::mutex commandMutex;
std::mutex chatMutex;
std::deque<std::string> chatQueue;

namespace
{
constexpr size_t kChatQueueLimit = 4096;
constexpr size_t kChatPumpLimit = 64;

constexpr DWORD kJumpUsingEnergyRva = 0x004EDC60 - 0x00400000;
constexpr DWORD kJumpPrimaryCallRva = 0x004EAEA2 - 0x00400000;
constexpr DWORD kJumpSecondaryCallRva = 0x004EB15C - 0x00400000;
constexpr DWORD kJumpThresholdEpsilonRva = 0x007B2DFC - 0x00400000;
constexpr DWORD kRollUsingEnergyRva = 0x004EDD30 - 0x00400000;
constexpr DWORD kSprintRollCallRva = 0x004EB146 - 0x00400000;
constexpr DWORD kSetNotPlayingRva = 0x005B9440 - 0x00400000;
constexpr DWORD kShellDropDisconnectCallRva = 0x005DDFDF - 0x00400000;
constexpr DWORD kPostLoadDisconnectCallRva = 0x005E42E4 - 0x00400000;
constexpr DWORD kWeaponDispenserFireRva = 0x00684C90 - 0x00400000;
constexpr DWORD kWeaponDispenserStrengthOffset = 0x114;

constexpr DWORD kControllableOffset = 0x240;
constexpr DWORD kEndSprintVtableOffset = 0xB0;
constexpr DWORD kForwardOffset = 0x110;
constexpr DWORD kVelocityOffset = 0x4DC;
constexpr DWORD kSoldierClassOffset = 0x440;
constexpr DWORD kEnergyFlagsOffset = 0xA18;
constexpr DWORD kNormalSpeedOffset = 0x69C;
constexpr DWORD kNormalJumpCostOffset = 0x7B4;
constexpr DWORD kSprintJumpCostOffset = 0x7B8;
constexpr float kLockedJumpProjectionGrace = 0.1f;

using JumpUsingEnergyFn = bool(__thiscall *)(void *);
using ApplyJumpStateFn = bool(__thiscall *)(void *);
using RollUsingEnergyFn = bool(__thiscall *)(void *);
using EndSprintFn = void(__thiscall *)(void *);
using SetNotPlayingFn = void(__cdecl *)(int);

struct ServerJumpParameters
{
	float normalCost;
	float sprintCost;
	float projection;
	float threshold;
};

JumpUsingEnergyFn serverJumpUsingEnergy;
RollUsingEnergyFn serverRollUsingEnergy;
SetNotPlayingFn serverSetNotPlaying;

} // namespace

// Write patchSize bytes from patch to the module-relative offset.
// Restore page protection and flush the instruction cache after the copy.
static void bf2server_patch_asm(DWORD_PTR offset, void *patch, size_t patchSize)
{
	DWORD op, np;
	DWORD addr = moduleBase + offset;
	VirtualProtect((void *)addr, patchSize, PAGE_EXECUTE_READWRITE, &op);
	memcpy((void *)addr, patch, patchSize);
	FlushInstructionCache(GetCurrentProcess(), (void *)addr, patchSize);
	VirtualProtect((void *)addr, patchSize, op, &np);
}

// Platform compatibility patches.
void bf2server_patch_norender()
{
	BYTE patch[] = {// push [ebp+8] -> nop
					0x90, 0x90, 0x90,

					// mov ecx, esi
					0x8B, 0xCE,

					// call 0x6bb440 -> nop
					0x90, 0x90, 0x90, 0x90, 0x90,

					// test al, al -> xor al, al
					0x30, 0xc0,

					// jnz 0x6BB3A6 -> nop
					0x90, 0x90};

	bf2server_patch_asm(OFFSET_NORENDER_FIX, (void *)patch, sizeof(patch));
}

void bf2server_patch_password()
{
	BYTE patch[] = {// push 0x01 -> nop
					0x90, 0x90,

					// push 0x80 -> nop
					0x90, 0x90, 0x90, 0x90, 0x90,

					// lea edx, [ebp-AC]
					// mov ecx, offset 007AA3F8
					//-> nop
					//-> lea eax, ds:1e31c40
					0x90, 0x90, 0x90, 0x90, 0x90, 0x8D, 0x05, 0x40, 0x1c, 0xe3, 0x01,

					// call 005A2380 -> nop
					0x90, 0x90, 0x90, 0x90, 0x90,

					// lea eax, [ebp-AC] -> nop
					0x90, 0x90, 0x90, 0x90, 0x90, 0x90};

	*(DWORD *)&patch[14] = OFFSET_PASSWORD_PLAIN + moduleBase;

	bf2server_patch_asm(OFFSET_PASSWORD_FIX, (void *)patch, sizeof(patch));
}

void bf2server_patch_dedicated()
{
	BYTE numPlayersPatch[] = {// push[esp + 0D0h + var_B8]
							  //-> push 1
							  //-> inc edi
							  //-> nop
							  0x6A, 0x01, 0x47, 0x90};

	BYTE serverTypePatch[] = {// push 1
							  //-> push 2
							  0x6A, 0x02};

	bf2server_patch_asm(OFFSET_NUMPLAYERS_MOD, (void *)numPlayersPatch, sizeof(numPlayersPatch));
	bf2server_patch_asm(OFFSET_DEDICATED_FIX, (void *)serverTypePatch, sizeof(serverTypePatch));
}

// Security and stability patches.
void bf2server_patch_votekick_exploit()
{
	BYTE crashPatch[] = {// mov cl, byte ptr[ebp]
						 //-> xor cl, cl
						 //-> nop
						 0x32, 0xC9, 0x90};

	BYTE kickPatch[] = {// call 005A22A0 -> nop
						0x90, 0x90, 0x90, 0x90, 0x90};

	bf2server_patch_asm(OFFSET_VOTECRASH_FIX, (void *)crashPatch, sizeof(crashPatch));
	bf2server_patch_asm(OFFSET_VOTEKICK_FIX, (void *)kickPatch, sizeof(kickPatch));
}

static void __declspec(naked) bf2server_mapfix_cc()
{
	__asm {
		mov eax, dword ptr[tickAddr]
		lock inc dword ptr[eax]

		cmp dword ptr[eax], MAPFIX_IDLE_TIMEOUT
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
	mapfixRetnAddr = OFFSET_MAPFIX_RETN + moduleBase;
	tickAddr = reinterpret_cast<DWORD>(&mapfixTicks);

	BYTE detourPatch[] = {// movzx eax, offset 01E64359
						  //-> mov eax, <ccAddr>
						  // jmp eax
						  0xB8, 0x00, 0x00, 0x00, 0x00, 0xff, 0xe0};

	*(DWORD *)&detourPatch[1] = (DWORD)&bf2server_mapfix_cc;
	bf2server_patch_asm(OFFSET_MAPFIX_DETOUR, detourPatch, sizeof(detourPatch));
}

// Network replication patches.
void bf2server_patch_distance_lag()
{
	BYTE playerMovesPatch[] = {// 0x5D38B8: MOV [EBP-0x1C],5 -> 32 nearest player moves.
							   0x20};

	bf2server_patch_asm(0x001d38b8, (void *)playerMovesPatch, sizeof(playerMovesPatch));
}

void bf2server_patch_waitlate_grace()
{
	// /waitlate accepts one preceding input turn instead of three.
	// 0x5BAAD0: grace immediate 3 -> 1; /nowaitlate remains 0.
	BYTE grace = 1;

	bf2server_patch_asm(0x005BAAD0 - 0x400000, (void *)&grace, sizeof(grace));
}

namespace
{
static DWORD g_objectBudgetResume;
static DWORD g_entityMineVtable;
static DWORD g_eventCollectResume;
static DWORD g_scoreNetEvent;
static DWORD g_findOrdnanceClass;
static DWORD g_entityMineClassRtti;

// EntityMine network-priority hooks.
// Reorder recurring state and CreateOrdnance candidates without bypassing
// either native packet budget.

struct NetEventHeapEntry
{
	float priority;
	DWORD index;
};

struct NetEventHeap
{
	DWORD count;
	DWORD capacity;
	NetEventHeapEntry *entries;
};

struct NetEventCandidate
{
	DWORD index;
	float score;
};

using ScoreNetEventFn = float(__cdecl *)(const void *);
using FindOrdnanceClassFn = void *(__cdecl *)(DWORD);
using IsRttiFn = bool(__thiscall *)(void *, DWORD);

bool is_entity_mine_event(const BYTE *event)
{
	if (*reinterpret_cast<const DWORD *>(event) != 0) return false;

	auto findClass = reinterpret_cast<FindOrdnanceClassFn>(g_findOrdnanceClass);
	void *ordnanceClass = findClass(*reinterpret_cast<const DWORD *>(event + 0x38));
	if (ordnanceClass == nullptr) return false;

	auto vtable = *reinterpret_cast<void ***>(ordnanceClass);
	auto isRtti = reinterpret_cast<IsRttiFn>(vtable[3]);
	return isRtti(ordnanceClass, *reinterpret_cast<DWORD *>(g_entityMineClassRtti));
}

void insert_net_event(NetEventHeap *heap, DWORD index, float priority)
{
	if (heap->count >= heap->capacity) return;

	DWORD position = ++heap->count;
	while (position > 1)
	{
		DWORD parent = position >> 1;
		if (priority <= heap->entries[parent].priority) break;
		heap->entries[position] = heap->entries[parent];
		position = parent;
	}
	heap->entries[position] = {priority, index};
}

// Collect eligible EntityMine CreateOrdnance events before other events.
// The native score filter, heap capacity, and packet writer still decide
// which candidates fit in the one-shot event section.
void __cdecl bf2_collect_prioritized_events(NetEventHeap *heap)
{
	NetEventCandidate mines[128];
	NetEventCandidate ordinary[128];
	DWORD mineCount = 0;
	DWORD ordinaryCount = 0;

	DWORD base = static_cast<DWORD>(moduleBase);
	DWORD destination = *reinterpret_cast<DWORD *>(base + 0x1BA9C2C);
	if (destination >= 64) return;
	DWORD index = *reinterpret_cast<DWORD *>(base + 0x1ACEF88 + destination * 0x208) & 0x7F;
	DWORD head = *reinterpret_cast<DWORD *>(base + 0x1BA9C40) & 0x7F;
	auto scoreEvent = reinterpret_cast<ScoreNetEventFn>(g_scoreNetEvent);

	DWORD scanned = 0;
	while (index != head && scanned < 128)
	{
		const BYTE *event = reinterpret_cast<const BYTE *>(base + 0x1BA5240 + index * 0x50);
		float score = scoreEvent(event);
		if (score >= 0.0f)
		{
			NetEventCandidate candidate = {index, score};
			if (is_entity_mine_event(event))
				mines[mineCount++] = candidate;
			else
				ordinary[ordinaryCount++] = candidate;
		}
		index = (index + 1) & 0x7F;
		++scanned;
	}

	for (DWORD candidate = 0; candidate < mineCount && heap->count < heap->capacity; ++candidate)
	{
		insert_net_event(heap, mines[candidate].index, 62501.0f - mines[candidate].score);
	}
	for (DWORD candidate = 0; candidate < ordinaryCount && heap->count < heap->capacity; ++candidate)
	{
		insert_net_event(heap, ordinary[candidate].index, -ordinary[candidate].score);
	}
}

// Stable-partition EntityMine records ahead of other recurring object state.
// The native writer still serializes each record by object handle and stops
// at the adjusted recurring-state budget.
void __cdecl bf2_prioritize_entity_mines(DWORD *objects, DWORD count)
{
	if (count > 64) return;

	DWORD ordered[64];
	DWORD output = 0;
	for (DWORD index = 0; index < count; ++index)
	{
		DWORD object = objects[index];
		if (object != 0 && *reinterpret_cast<DWORD *>(object) == g_entityMineVtable) ordered[output++] = object;
	}
	for (DWORD index = 0; index < count; ++index)
	{
		DWORD object = objects[index];
		if (object == 0 || *reinterpret_cast<DWORD *>(object) != g_entityMineVtable) ordered[output++] = object;
	}
	for (DWORD index = 0; index < count; ++index) objects[index] = ordered[index];
}

// 0x5CE71C: replace the recurring-state budget calculation in WriteObjects.
// Move EntityMine records first; reserve 32 bytes per queued event after 3,
// capped at 200 bytes, then replay the native scale math at 0x5CE732.
void __declspec(naked) bf2_object_budget_cc()
{
	__asm {
		pushad
		mov eax, dword ptr [moduleBase]
		mov ecx, dword ptr [eax+1ba9c58h]
		lea edx, [ebp-308h]
		push ecx
		push edx
		call bf2_prioritize_entity_mines
		add esp, 8
		popad

		push ecx

		mov eax, dword ptr [moduleBase]
		mov edx, dword ptr [eax+1ba9c2ch]
		cmp edx, 40h
		jae no_reserve
		imul edx, edx, 208h
		mov ecx, dword ptr [eax+edx+1acef88h]
		mov edx, dword ptr [eax+1ba9c40h]
		sub edx, ecx
		and edx, 7fh

		cmp edx, 3
		jbe no_reserve
		sub edx, 3
		imul edx, edx, 20h
		cmp edx, 0c8h
		jbe reserve_ready
		mov edx, 0c8h
		jmp reserve_ready

	no_reserve:
		xor edx, edx

	reserve_ready:
		mov ecx, 320h
		sub ecx, edx
		mov eax, dword ptr [eax+3e9268h]
		imul eax, ecx
		cdq
		and edx, 3ffh
		add eax, edx
		sar eax, 0ah

		pop ecx
		jmp dword ptr [g_objectBudgetResume]
	}
}

// 0x5BFDAE: replace the native 64-event score/heap scan.
// Insert eligible EntityMine CreateOrdnance first, then ordinary candidates;
// resume the native packet-budgeted writer at 0x5BFE4A.
void __declspec(naked) bf2_event_collect_cc()
{
	__asm {
		pushad
		lea eax, [ebp-30h]
		push eax
		call bf2_collect_prioritized_events
		add esp, 4
		popad
		jmp dword ptr [g_eventCollectResume]
	}
}

} // namespace

// Install both EntityMine priority paths.
// Persistent object state and one-shot creates retain their separate budgets.
void bf2server_patch_object_budget()
{
	g_objectBudgetResume = static_cast<DWORD>(moduleBase + 0x005CE732 - 0x400000);
	g_entityMineVtable = static_cast<DWORD>(moduleBase + 0x0079D214 - 0x400000);
	g_eventCollectResume = static_cast<DWORD>(moduleBase + 0x005BFE4A - 0x400000);
	g_scoreNetEvent = static_cast<DWORD>(moduleBase + 0x005BF820 - 0x400000);
	g_findOrdnanceClass = static_cast<DWORD>(moduleBase + 0x005D46C0 - 0x400000);
	g_entityMineClassRtti = static_cast<DWORD>(moduleBase + 0x01EBD444 - 0x400000);

	// 0x5CE71C: state-budget block -> JMP bf2_object_budget_cc.
	// The detour reproduces the skipped scale calculation before resuming.
	BYTE detour[] = {0xE9, 0x00, 0x00, 0x00, 0x00};
	DWORD site = static_cast<DWORD>(moduleBase + 0x005CE71C - 0x400000);
	*reinterpret_cast<DWORD *>(&detour[1]) = reinterpret_cast<DWORD>(&bf2_object_budget_cc) - (site + sizeof(detour));
	bf2server_patch_asm(0x005CE71C - 0x400000, detour, sizeof(detour));

	// 0x5BFDAE: 64-candidate scan -> JMP bf2_event_collect_cc.
	// The detour rebuilds the native heap with EntityMine creates first.
	BYTE eventDetour[] = {0xE9, 0x00, 0x00, 0x00, 0x00};
	DWORD eventSite = static_cast<DWORD>(moduleBase + 0x005BFDAE - 0x400000);
	*reinterpret_cast<DWORD *>(&eventDetour[1]) =
		reinterpret_cast<DWORD>(&bf2_event_collect_cc) - (eventSite + sizeof(eventDetour));
	bf2server_patch_asm(0x005BFDAE - 0x400000, eventDetour, sizeof(eventDetour));
}

namespace
{
// 30 UPS send-scheduling hooks.
// Naked detours read the WriteObjects, scheduler, and SentUpdate EBP frames.

constexpr DWORD kNetPlayerCount = 0x40;
constexpr DWORD kNetPlayerStride = 0x208;
constexpr DWORD kPendingMapOffset = 0x4;
constexpr DWORD kMapTurnOffset = 0x104;
constexpr DWORD kSrttOffset = 0x1C4;
constexpr DWORD kRttVarOffset = 0x1C8;
constexpr float kCreateRetryFloor = 0.1f;

struct CreateFence
{
	void *pendingMap;
	int pendingTurn;
	float sentTime;
};
static_assert(sizeof(CreateFence) == 0xC, "CreateFence assembly stride mismatch");

using NetObjStateMapAllocFn = void *(__cdecl *)();
using NetObjStateMapFreeFn = void(__thiscall *)(void *);

static DWORD g_curDstAddr;		 // &_curDst (dest client index)        VA 0x01FA9C2C
static DWORD g_netPlayersAddr;	 // per-client state                     VA 0x01ECEF50
static DWORD g_netTimeAddr;		 // NetComm time                         VA 0x01FA5214
static DWORD g_wo_resume;		 // WriteObjects resume after MOV        VA 0x005CE58C
static DWORD g_send_resume;		 // scheduler continue                   VA 0x005C9D5D
static DWORD g_send_skip_resume; // scheduler next-client                 VA 0x005C9C98
static DWORD g_su2_time_resume;	 // SentUpdate post-time-call resume     VA 0x005D2DF6
static DWORD g_su2_skip_resume;	 // SentUpdate skip-slot resume          VA 0x005D2E21
static DWORD g_getTimeFn;		 // time function                        VA 0x005B3840
static CreateFence g_createFences[kNetPlayerCount];
static NetObjStateMapAllocFn g_allocNetObjStateMap;
static NetObjStateMapFreeFn g_freeNetObjStateMap;

static void clear_create_fence(CreateFence &fence)
{
	fence = {};
}

// Hold a destination while map B still identifies the emitted CREATE turn.
// Native ACK/NACK handling replaces or resets B; a timeout performs the same
// pending-map reset as the native NACK path so the CREATE can be regenerated.
bool __cdecl bf2_create_fence_blocks(int player)
{
	if (static_cast<DWORD>(player) >= kNetPlayerCount)
	{
		return false;
	}

	auto &fence = g_createFences[player];
	if (fence.pendingMap == nullptr)
	{
		return false;
	}

	auto playerState = reinterpret_cast<BYTE *>(g_netPlayersAddr + player * kNetPlayerStride);
	auto pendingMap = *reinterpret_cast<void **>(playerState + kPendingMapOffset);

	if (pendingMap != fence.pendingMap ||
		*reinterpret_cast<int *>(static_cast<BYTE *>(pendingMap) + kMapTurnOffset) != fence.pendingTurn)
	{
		clear_create_fence(fence);
		return false;
	}

	// Match the native send-window timeout, with a 100 ms retry floor.
	float retryTime =
		*reinterpret_cast<float *>(playerState + kSrttOffset) + *reinterpret_cast<float *>(playerState + kRttVarOffset);
	if (!(retryTime >= kCreateRetryFloor))
	{
		retryTime = kCreateRetryFloor;
	}

	float elapsed = *reinterpret_cast<float *>(g_netTimeAddr) - fence.sentTime;
	if (!(elapsed > retryTime))
	{
		return true;
	}

	// ReadSwitchResponses result 2 frees B and installs a fresh pending map.
	g_freeNetObjStateMap(pendingMap);
	*reinterpret_cast<void **>(playerState + kPendingMapOffset) = g_allocNetObjStateMap();
	clear_create_fence(fence);
	return false;
}

// 0x5CE582: replace MOV [EBP-0x84],6 after the object-state writer.
// If [EBP-0x11] reports a CREATE, record the destination's map B, turn stamp,
// and send time; replay the MOV and resume at 0x5CE58C.
void __declspec(naked) bf2_create_fence_cc()
{
	__asm {
		pushfd
		push  eax
		push  ecx
		push  edx
		cmp   byte ptr [ebp-11h], 0 // WriteObjects emitted-CREATE flag
		jz    create_done
		mov   eax, dword ptr [g_curDstAddr]
		mov   eax, dword ptr [eax]
		cmp   eax, 40h
		jae   create_done
		mov   ecx, eax
		imul  ecx, 0Ch
		lea   edx, g_createFences
		add   edx, ecx
		imul  eax, eax, 208h
		mov   ecx, dword ptr [g_netPlayersAddr]
		mov   ecx, dword ptr [ecx+eax+4]
		mov   dword ptr [edx], ecx
		mov   eax, dword ptr [ecx+104h]
		mov   dword ptr [edx+4], eax
		mov   eax, dword ptr [g_netTimeAddr]
		mov   eax, dword ptr [eax]
		mov   dword ptr [edx+8], eax
	create_done:
		pop   edx
		pop   ecx
		pop   eax
		popfd
		mov   dword ptr [ebp-84h], 6
		mov   eax, dword ptr [g_wo_resume]
		jmp   eax
	}
}

// 0x5C9D56: replace the JNZ/JMP after IsSendWindowOpen.
// Skip this destination while its CREATE fence remains; otherwise preserve
// the high-UPS bypass and resume at 0x5C9D5D or scan at 0x5C9C98.
void __declspec(naked) bf2_create_fence_gate_cc()
{
	__asm {
		pushad
		mov   eax, dword ptr [ebp-10h]
		push  eax
		call  bf2_create_fence_blocks
		add   esp, 4
		test  al, al
		popad
		jnz   create_blocked
		mov   eax, dword ptr [g_send_resume]
		jmp   eax
	create_blocked:
		mov   eax, dword ptr [g_send_skip_resume]
		jmp   eax
	}
}

// 0x5D2DF1: replace CALL GetTime in SentUpdate's two-slot tracker.
// Skip the native write when [EBP-0x4] == 2 instead of indexing slot 2;
// otherwise replay the call and resume at 0x5D2DF6 or 0x5D2E21.
void __declspec(naked) bf2_su2_slotfix_cc()
{
	__asm {
		cmp   dword ptr [ebp-4], 2 // local_4: free slot idx, or 2 if none free
		jb    su2_domark
		mov   eax, dword ptr [g_su2_skip_resume] // no free slot -> skip marking (0x5D2E21)
		jmp   eax
	su2_domark:
		call  dword ptr [g_getTimeFn] // redo CALL 0x5b3840 -> XMM0 = now (secs)
		jmp   dword ptr [g_su2_time_resume] // resume 0x5D2DF6 (native MOVSS)
	}
}

// Install one-turn scheduling with a per-destination CREATE transaction fence.
// IsPipeFull and native switch-response parsing remain unchanged; only the
// old interval-only CREATE delay and IsSendWindowOpen branch are replaced.
void bf2server_patch_send_scheduling()
{
	g_curDstAddr = (DWORD)(moduleBase + 0x1BA9C2C);		 // _curDst          0x01FA9C2C
	g_netPlayersAddr = (DWORD)(moduleBase + 0x1ACEF50);	 // player state     0x01ECEF50
	g_netTimeAddr = (DWORD)(moduleBase + 0x1BA5214);	 // NetComm time     0x01FA5214
	g_wo_resume = (DWORD)(moduleBase + 0x1CE58C);		 // WO resume        0x005CE58C
	g_send_resume = (DWORD)(moduleBase + 0x1C9D5D);		 // scheduler resume 0x005C9D5D
	g_send_skip_resume = (DWORD)(moduleBase + 0x1C9C98); // scheduler skip 0x005C9C98
	g_su2_time_resume = (DWORD)(moduleBase + 0x1D2DF6);	 // SU2 native MOVSS 0x005D2DF6
	g_su2_skip_resume = (DWORD)(moduleBase + 0x1D2E21);	 // SU2 skip-mark    0x005D2E21
	g_getTimeFn = (DWORD)(moduleBase + 0x1B3840);		 // get-now fn       0x005B3840
	g_allocNetObjStateMap = reinterpret_cast<NetObjStateMapAllocFn>(moduleBase + 0x1B5670);
	g_freeNetObjStateMap = reinterpret_cast<NetObjStateMapFreeFn>(moduleBase + 0x1B5720);
	memset(g_createFences, 0, sizeof(g_createFences));

	// 0x5D2DF1: CALL GetTime -> JMP bf2_su2_slotfix_cc.
	// Prevent SentUpdate from writing beyond its two acknowledgement slots.
	BYTE d[] = {0xE9, 0x00, 0x00, 0x00, 0x00};
	DWORD dSite = static_cast<DWORD>(moduleBase + 0x005D2DF1 - 0x400000);
	*(DWORD *)&d[1] = reinterpret_cast<DWORD>(&bf2_su2_slotfix_cc) - (dSite + sizeof(d));
	bf2server_patch_asm(0x005D2DF1 - 0x400000, d, sizeof(d));

	// 0x5CE582: MOV [EBP-0x84],6 -> JMP bf2_create_fence_cc.
	// Arm the fence only when this completed WriteObjects pass emitted CREATE.
	BYTE a[] = {0xE9, 0x00, 0x00, 0x00, 0x00};
	DWORD aSite = static_cast<DWORD>(moduleBase + 0x005CE582 - 0x400000);
	*(DWORD *)&a[1] = reinterpret_cast<DWORD>(&bf2_create_fence_cc) - (aSite + sizeof(a));
	bf2server_patch_asm(0x005CE582 - 0x400000, a, sizeof(a));

	// 0x5C9D56: JNZ/JMP -> JMP bf2_create_fence_gate_cc; NOP x2.
	// Block all ordinary updates between CREATE and its map-switch response.
	BYTE b[] = {0xE9, 0x00, 0x00, 0x00, 0x00, 0x90, 0x90};
	DWORD bSite = static_cast<DWORD>(moduleBase + 0x005C9D56 - 0x400000);
	*(DWORD *)&b[1] = reinterpret_cast<DWORD>(&bf2_create_fence_gate_cc) - (bSite + 5);
	bf2server_patch_asm(0x005C9D56 - 0x400000, b, sizeof(b));

	// Keep unfenced destinations eligible again on the following server turn.
	// 0x5D2E8F: ADD ECX,[EBP-0x18] -> ADD ECX,1.
	BYTE c[] = {0x83, 0xC1, 0x01};
	bf2server_patch_asm(0x005D2E8F - 0x400000, c, sizeof(c));
}

} // namespace

// 30 UPS network-update patch set.
// Remove render/sleep throttles, visit all clients, and use the CREATE-aware
// scheduler while retaining the native IsPipeFull capacity check.
void bf2server_patch_netupdate()
{
	// 0x5338FA: CALL dedicated render/present -> NOP x5.
	BYTE render_patch[] = {0x90, 0x90, 0x90, 0x90, 0x90};
	bf2server_patch_asm(0x005338FA - 0x400000, reinterpret_cast<void *>(render_patch), sizeof(render_patch));

	// 0x618B03: PUSH 10 -> PUSH 0 for inactive-window Sleep.
	BYTE window_sleep_patch[] = {0x6A, 0x00};
	bf2server_patch_asm(0x00618B03 - 0x400000, reinterpret_cast<void *>(window_sleep_patch),
						sizeof(window_sleep_patch));

	// 0x5C9C19: remove signed /2 from the netCurMaxPlayers send budget.
	BYTE send_all_patch[] = {0x90, 0x90, 0x90, 0x90, 0x90};
	bf2server_patch_asm(OFFSET_UPS_CLIENT_LIMITER, reinterpret_cast<void *>(send_all_patch), sizeof(send_all_patch));

	// Apply one-turn scheduling with acknowledgement fencing for CREATE updates.
	bf2server_patch_send_scheduling();

	// 0x5C9D40: retain the stock IsPipeFull skip.
}

// Weapons and movement patches.
namespace
{
static DWORD g_weaponDispenserFireResume;

// WeaponDispenser throw-strength clamp.
// Clamp Weapon+0x114 before every server-side dispenser launch.

void __cdecl bf2_clamp_dispenser_strength(void *weapon)
{
	float &strength = *reinterpret_cast<float *>(static_cast<BYTE *>(weapon) + kWeaponDispenserStrengthOffset);
	if (!std::isfinite(strength) || strength < 0.0f)
		strength = 0.0f;
	else if (strength > 1.0f)
		strength = 1.0f;
}

// 0x684C90: replace the six-byte WeaponDispenser::Fire prologue.
// Clamp strength to [0,1], replay PUSH EBP/MOV EBP,ESP/AND ESP,-16,
// and resume at 0x684C96.
void __declspec(naked) bf2_dispenser_fire_cc()
{
	__asm {
		push ecx
		push ecx
		call bf2_clamp_dispenser_strength
		add esp, 4
		pop ecx

		push ebp
		mov ebp, esp
		and esp, 0fffffff0h
		jmp dword ptr [g_weaponDispenserFireResume]
	}
}

} // namespace

void bf2server_patch_speedpacks()
{
	g_weaponDispenserFireResume = static_cast<DWORD>(moduleBase + kWeaponDispenserFireRva + 6);
	BYTE detour[] = {0xE9, 0x00, 0x00, 0x00, 0x00, 0x90};
	DWORD site = static_cast<DWORD>(moduleBase + kWeaponDispenserFireRva);

	// 0x684C90: Fire prologue -> JMP bf2_dispenser_fire_cc; NOP byte 6.
	*reinterpret_cast<DWORD *>(&detour[1]) = reinterpret_cast<DWORD>(&bf2_dispenser_fire_cc) - (site + 5);
	bf2server_patch_asm(kWeaponDispenserFireRva, detour, sizeof(detour));
}

namespace
{
template <typename T> T read_server_field(const void *object, DWORD offset)
{
	return *reinterpret_cast<const T *>(static_cast<const BYTE *>(object) + offset);
}

ServerJumpParameters capture_server_jump_parameters(void *soldier)
{
	auto soldierClass = read_server_field<const BYTE *>(soldier, kSoldierClassOffset);
	float projection =
		read_server_field<float>(soldier, kVelocityOffset) * read_server_field<float>(soldier, kForwardOffset) +
		read_server_field<float>(soldier, kVelocityOffset + 4) * read_server_field<float>(soldier, kForwardOffset + 4) +
		read_server_field<float>(soldier, kVelocityOffset + 8) * read_server_field<float>(soldier, kForwardOffset + 8);
	float threshold = read_server_field<float>(soldierClass, kNormalSpeedOffset) +
					  *reinterpret_cast<float *>(moduleBase + kJumpThresholdEpsilonRva);
	float normalCost = read_server_field<float>(soldierClass, kNormalJumpCostOffset);
	float sprintCost = read_server_field<float>(soldierClass, kSprintJumpCostOffset);
	return {normalCost, sprintCost, projection, threshold};
}

bool use_locked_jump_fallback(void *soldier)
{
	auto parameters = capture_server_jump_parameters(soldier);
	return (read_server_field<DWORD>(soldier, kEnergyFlagsOffset) & 1) != 0 && parameters.normalCost <= 0.0f &&
		   parameters.sprintCost > parameters.normalCost && parameters.projection > parameters.threshold &&
		   parameters.projection <= parameters.threshold + kLockedJumpProjectionGrace;
}

// 0x4EDCFA: call the Controllable +0xA4 jump-state virtual without Energy::Use.
bool apply_server_jump_state(void *soldier)
{
	auto controllable = static_cast<BYTE *>(soldier) + kControllableOffset;
	auto vtable = *reinterpret_cast<void ***>(controllable);
	auto applyJump = reinterpret_cast<ApplyJumpStateFn>(vtable[0xA4 / sizeof(void *)]);
	return applyJump(controllable);
}

bool __fastcall server_jump_cc(void *soldier, void *)
{
	return use_locked_jump_fallback(soldier) ? apply_server_jump_state(soldier) : serverJumpUsingEnergy(soldier);
}

} // namespace

void bf2server_patch_locked_jump()
{
#ifdef GALAXY
	serverJumpUsingEnergy = reinterpret_cast<JumpUsingEnergyFn>(moduleBase + kJumpUsingEnergyRva);
	BYTE primaryPatch[] = {0xE8, 0x00, 0x00, 0x00, 0x00};
	BYTE secondaryPatch[] = {0xE8, 0x00, 0x00, 0x00, 0x00};
	DWORD primaryAddress = static_cast<DWORD>(moduleBase + kJumpPrimaryCallRva);
	DWORD secondaryAddress = static_cast<DWORD>(moduleBase + kJumpSecondaryCallRva);
	*reinterpret_cast<DWORD *>(&primaryPatch[1]) = reinterpret_cast<DWORD>(&server_jump_cc) - (primaryAddress + 5);
	*reinterpret_cast<DWORD *>(&secondaryPatch[1]) = reinterpret_cast<DWORD>(&server_jump_cc) - (secondaryAddress + 5);

	// Low-stamina jump fallback at both server jump call sites.
	// 0x4EAEA2/0x4EB15C: CALL JumpUsingEnergy -> CALL server_jump_cc;
	// the wrapper preserves stock handling outside the locked-jump state.
	bf2server_patch_asm(kJumpPrimaryCallRva, primaryPatch, sizeof(primaryPatch));
	bf2server_patch_asm(kJumpSecondaryCallRva, secondaryPatch, sizeof(secondaryPatch));
#endif
}

namespace
{
void apply_server_end_sprint(void *soldier)
{
	auto controllable = static_cast<BYTE *>(soldier) + kControllableOffset;
	auto vtable = *reinterpret_cast<void ***>(controllable);
	auto endSprint = reinterpret_cast<EndSprintFn>(vtable[kEndSprintVtableOffset / sizeof(void *)]);
	endSprint(controllable);
}

bool __fastcall server_roll_cc(void *soldier, void *)
{
	const bool rolled = serverRollUsingEnergy(soldier);
	if (!rolled && (read_server_field<DWORD>(soldier, kEnergyFlagsOffset) & 1) != 0)
	{
		apply_server_end_sprint(soldier);
	}
	return rolled;
}

} // namespace

void bf2server_patch_infinite_sprint()
{
#ifdef GALAXY
	serverRollUsingEnergy = reinterpret_cast<RollUsingEnergyFn>(moduleBase + kRollUsingEnergyRva);
	BYTE callPatch[] = {0xE8, 0x00, 0x00, 0x00, 0x00};
	DWORD callAddress = static_cast<DWORD>(moduleBase + kSprintRollCallRva);
	*reinterpret_cast<DWORD *>(&callPatch[1]) = reinterpret_cast<DWORD>(&server_roll_cc) - (callAddress + 5);

	// Clear sprint state when its RollUsingEnergy attempt is rejected.
	// 0x4EB146: CALL RollUsingEnergy -> CALL server_roll_cc;
	// the wrapper calls EndSprint only after a failed sprint-state roll.
	bf2server_patch_asm(kSprintRollCallRva, callPatch, sizeof(callPatch));
#endif
}

// Player lifecycle and spawning patches.
static void __fastcall preplay_disconnect_cc(int player, void *)
{
	serverSetNotPlaying(player);
}

void bf2server_patch_preplay_disconnect()
{
#ifdef GALAXY
	serverSetNotPlaying = reinterpret_cast<SetNotPlayingFn>(moduleBase + kSetNotPlayingRva);
	BYTE callPatch[] = {0xE8, 0x00, 0x00, 0x00, 0x00};
	DWORD callAddress = static_cast<DWORD>(moduleBase + kShellDropDisconnectCallRva);

	// Remove abandoned players from next-playing membership before a match.
	// 0x5DDFDF/0x5E42E4: CALL SetNotPlaying -> CALL preplay_disconnect_cc
	// in both shell and post-load disconnect paths.
	*reinterpret_cast<DWORD *>(&callPatch[1]) = reinterpret_cast<DWORD>(&preplay_disconnect_cc) - (callAddress + 5);
	bf2server_patch_asm(kShellDropDisconnectCallRva, callPatch, sizeof(callPatch));

	callAddress = static_cast<DWORD>(moduleBase + kPostLoadDisconnectCallRva);
	*reinterpret_cast<DWORD *>(&callPatch[1]) = reinterpret_cast<DWORD>(&preplay_disconnect_cc) - (callAddress + 5);
	bf2server_patch_asm(kPostLoadDisconnectCallRva, callPatch, sizeof(callPatch));
#endif
}

void bf2server_patch_spawnvalue()
{
	char *envBuffer = nullptr;
	size_t envSize = 0;
	if (_dupenv_s(&envBuffer, &envSize, "SPAWN_TIMER") == 0 && envBuffer != nullptr)
	{
		char *end = nullptr;
		const float parsed = std::strtof(envBuffer, &end);
		if (end != envBuffer && std::isfinite(parsed) && parsed >= 0.0f)
		{
			spawnValue = parsed;
		}
	}
	free(envBuffer);
	spawnValueAddr = reinterpret_cast<DWORD>(&spawnValue);

	// Configure the normal respawn-wave delay from SPAWN_TIMER.
	// 0x58D609: stock 15.0f operand -> spawnValue.
	bf2server_patch_asm(OFFSET_SPAWNVALUE_MOD_FLOAT, (void *)&spawnValueAddr, sizeof(DWORD));
}

// Warmup-to-play spawn transition.
// Vanish players, reset each team timer, and require active characters to
// enter the wave after the transition instead of jump-spawning at zero.
static void __cdecl bf2server_pregame_spawn_cc()
{
	auto vanishAllPlayers = reinterpret_cast<void(__cdecl *)()>(moduleBase + OFFSET_VANISH_ALL_PLAYERS);
	vanishAllPlayers();

	auto spawnManager = *reinterpret_cast<BYTE **>(moduleBase + OFFSET_SPAWN_MANAGER);
	if (spawnManager == nullptr)
	{
		return;
	}

	// SpawnManager+0x5C/+0x7C configure runtime timers at +0x9C/+0xDC.
	for (DWORD team = 0; team < 8; ++team)
	{
		DWORD teamOffset = team * sizeof(FLOAT);
		FLOAT cycleDelay = *reinterpret_cast<FLOAT *>(spawnManager + 0x5C + teamOffset);
		FLOAT slotDelay = *reinterpret_cast<FLOAT *>(spawnManager + 0x7C + teamOffset);
		*reinterpret_cast<FLOAT *>(spawnManager + 0x9C + teamOffset) = cycleDelay;
		*reinterpret_cast<FLOAT *>(spawnManager + 0xDC + teamOffset) = slotDelay;
	}

	auto isPlaying = reinterpret_cast<bool(__cdecl *)(int, bool)>(moduleBase + OFFSET_IS_PLAYING);
	auto findCharacter = reinterpret_cast<BYTE *(__fastcall *)(int)>(moduleBase + OFFSET_FIND_CHARACTER);
	int maxPlayers = *reinterpret_cast<int *>(moduleBase + OFFSET_NET_CUR_MAX_PLAYERS);
	if (maxPlayers > 64)
	{
		maxPlayers = 64;
	}

	// SpawnManager+0xBC is the team wave; Character+0x15C is its required wave.
	for (int player = 0; player < maxPlayers; ++player)
	{
		if (!isPlaying(player, false))
		{
			continue;
		}

		BYTE *character = findCharacter(player);
		if (character == nullptr)
		{
			continue;
		}

		int team = *reinterpret_cast<int *>(character + 0x134);
		if (team < 0 || team >= 8)
		{
			continue;
		}

		DWORD teamOffset = static_cast<DWORD>(team) * sizeof(DWORD);
		int wave = *reinterpret_cast<int *>(spawnManager + 0xBC + teamOffset);
		*reinterpret_cast<int *>(character + 0x15C) = wave + 1;
	}
}

void bf2server_patch_pregame_spawn()
{
	// End pregame at equality instead of leaving one zero-time iteration.
	// 0x5C4F35: JLE -> JL.
	BYTE endAtZero = 0x7C;
	bf2server_patch_asm(OFFSET_PREGAME_END_BRANCH, (void *)&endAtZero, sizeof(endAtZero));

	// Apply the timer and required-wave reset during the native transition.
	// 0x5C4F37: CALL VanishAllPlayers -> CALL transition wrapper.
	BYTE callPatch[] = {0xE8, 0x00, 0x00, 0x00, 0x00};
	DWORD callAddress = static_cast<DWORD>(moduleBase + OFFSET_PREGAME_VANISH_CALL);
	*(DWORD *)&callPatch[1] = reinterpret_cast<DWORD>(&bf2server_pregame_spawn_cc) - (callAddress + sizeof(callPatch));
	bf2server_patch_asm(OFFSET_PREGAME_VANISH_CALL, (void *)callPatch, sizeof(callPatch));
}

// RCON and game integration.
std::string bf2server_command(DWORD messageType, DWORD sender, const wchar_t *message, DWORD responseOutput)
{
	std::lock_guard<std::mutex> lock(commandMutex);
	DWORD adminAccessAddr = moduleBase + OFFSET_LOGGED_IN;
	DWORD outputDetailsAddr = moduleBase + OFFSET_COMMAND_DETAILS;
	DWORD addr = moduleBase + OFFSET_CHATINPUT;
	BYTE previousAdminAccess = *(BYTE *)adminAccessAddr;
	BYTE previousOutputDetails = *(BYTE *)outputDetailsAddr;
	*(BYTE *)adminAccessAddr = 1;
	*(BYTE *)outputDetailsAddr = 1;
	__asm {
		push messageType
		push sender
		mov edx, message
		mov ecx, responseOutput
		call dword ptr[addr];
		add esp, 8
	}
	*(BYTE *)adminAccessAddr = previousAdminAccess;
	*(BYTE *)outputDetailsAddr = previousOutputDetails;
	addr = moduleBase + OFFSET_RESBUFFER;
	return std::string((char *)(addr));
}

void bf2server_set_chat_cc()
{
	chatCCAddr = reinterpret_cast<DWORD>(&bf2server_chat_cc);
	auto addr = reinterpret_cast<DWORD>(&chatCCAddr);
	// replace function pointer to snprintf with our own
	bf2server_patch_asm(OFFSET_CHATSNPRINTF, (void *)&addr, sizeof(DWORD));
}

int __cdecl bf2server_chat_cc(char *buf, size_t sz, const char *fmt, ...)
{
	int ret = -1;

	va_list args;
	va_start(args, fmt);
	ret = vsnprintf(buf, sz, fmt, args);
	va_end(args);
	if (buf != nullptr && sz > 0)
	{
		buf[sz - 1] = 0;
		std::lock_guard<std::mutex> lock(chatMutex);
		if (chatCB != NULL)
		{
			if (chatQueue.size() >= kChatQueueLimit) chatQueue.pop_front();
			chatQueue.emplace_back(buf);
		}
	}
	return ret;
}

std::string bf2server_get_adminpwd()
{
	DWORD addr = moduleBase + OFFSET_ADMINPW;
	return std::string((char *)addr);
}

std::wstring bf2server_s2ws(std::string const &s)
{
	if (s.empty()) return {};
	const int length = MultiByteToWideChar(CP_ACP, 0, s.data(), static_cast<int>(s.size()), nullptr, 0);
	if (length <= 0) return {};
	std::wstring result(static_cast<size_t>(length), L'\0');
	if (MultiByteToWideChar(CP_ACP, 0, s.data(), static_cast<int>(s.size()), &result[0], length) != length) return {};
	return result;
}

void bf2server_set_chat_cb(std::function<void(std::string const &msg)> onChat)
{
	std::lock_guard<std::mutex> lock(chatMutex);
	chatCB = std::move(onChat);
	if (chatCB == NULL) chatQueue.clear();
}

bool bf2server_pump_chat()
{
	std::deque<std::string> pending;
	std::function<void(std::string const &)> callback;
	bool empty;
	{
		std::lock_guard<std::mutex> lock(chatMutex);
		if (chatCB == NULL)
		{
			chatQueue.clear();
			return true;
		}
		size_t count = chatQueue.size() < kChatPumpLimit ? chatQueue.size() : kChatPumpLimit;
		while (count-- > 0)
		{
			pending.emplace_back(std::move(chatQueue.front()));
			chatQueue.pop_front();
		}
		empty = chatQueue.empty();
		callback = chatCB;
	}
	for (const std::string &message : pending) callback(message);
	return empty;
}

USHORT bf2server_get_gameport()
{
	DWORD addr = moduleBase + OFFSET_GAMEPORT;
	return *(USHORT *)addr;
}

MapStatus bf2server_get_map_status()
{
	DWORD addr = moduleBase + OFFSET_MAP_STATUS;
	return (MapStatus) * (BYTE *)addr;
}

bool bf2server_idle()
{
	DWORD addr = moduleBase + OFFSET_IDLE;
	return (*(BYTE *)addr) == 1;
}

void bf2server_mapfix_tick()
{
	if (bf2server_get_map_status() == MAP_IDLE)
	{
		InterlockedExchange(&mapfixTicks, 0);
	}
}

int bf2server_lua_dostring(std::string const &code)
{
	auto s = reinterpret_cast<DWORD>(code.c_str());
	auto l = static_cast<DWORD>(code.size());
	auto L = *(reinterpret_cast<DWORD *>(moduleBase + OFFSET_LUA_STATE));
	if (L == 0) return -1;
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

	if (res == LUA_OK)
	{
		__asm {
			push 0
			push 0
			push 0
			push L
			call dword ptr [lua_pcall]
			add esp, 16
			mov res, eax
		}
	}
	return static_cast<int>(res);
}

// Patch installation order.
void bf2server_init()
{
	Logger.log(LogLevel_VERBOSE, "Patching BattlefrontII process...");

	moduleBase = (DWORD)GetModuleHandleA("BattlefrontII.exe");

	bf2server_patch_norender();
	bf2server_patch_password();
	bf2server_patch_dedicated();

	bf2server_patch_votekick_exploit();
	bf2server_patch_maphang();

	bf2server_patch_distance_lag();
	bf2server_patch_waitlate_grace();
	bf2server_patch_object_budget();
	bf2server_patch_netupdate();

	bf2server_patch_speedpacks();
	bf2server_patch_locked_jump();
	bf2server_patch_infinite_sprint();
	bf2server_patch_preplay_disconnect();

	bf2server_patch_spawnvalue();
	bf2server_patch_pregame_spawn();

	bf2server_set_chat_cc();
	Logger.log(LogLevel_VERBOSE, "All patches applied.");
}
