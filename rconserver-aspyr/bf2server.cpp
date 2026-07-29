#include "bf2server.h"

#include "Logger.h"
#include "PatchEngine.h"

#include <algorithm>
#include <array>
#include <atomic>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <deque>
#include <intrin.h>
#include <memory>
#include <mutex>
#include <string_view>

namespace
{
HMODULE g_module{};
std::uintptr_t g_base{};
float g_spawnValue = 15.0f;

std::mutex g_commandMutex;
std::mutex g_chatMutex;
std::deque<std::string> g_chatQueue;
std::function<void(std::string const &)> g_chatCallback;
std::atomic_uint32_t g_mapHangTicks{};
std::unique_ptr<PatchEngine> g_patcher;

constexpr std::size_t NET_PLAYER_STRIDE = 0x200;
constexpr std::size_t NET_PLAYER_PENDING_MAP = 0x8;
constexpr std::size_t NET_PLAYER_EVENT_CURSOR = 0x3C;
constexpr std::size_t NET_PLAYER_TEAM = 0x154;
constexpr std::size_t NET_PLAYER_SPAWN_TICKET = 0x158;
constexpr std::size_t NET_PLAYER_SRTT = 0x1BC;
constexpr std::size_t NET_PLAYER_RTTVAR = 0x1C0;
constexpr std::size_t NETOBJ_MAP_TURN = 0x208;
constexpr std::size_t ORDINARY_EVENT_STRIDE = 0x70;
constexpr std::size_t ORDINARY_EVENT_CLASS = 0x50;
constexpr std::size_t WEAPON_DISPENSER_STRENGTH = 0x184;
constexpr std::size_t SOLDIER_CONTROLLABLE = 0x380;
constexpr std::size_t SOLDIER_ENERGY_FLAGS = 0xE04;
constexpr std::size_t END_SPRINT_VTABLE = 0x168;
constexpr int MAX_SCOPED_OBJECTS = 64;
constexpr int MAX_ORDINARY_EVENTS = ORDINARY_EVENT_RING_MASK + 1;
constexpr int MAX_SELECTED_EVENTS = 64;
constexpr float ENTITY_MINE_PRIORITY_BASE = 62501.0f;
constexpr float CREATE_RETRY_FLOOR = 0.1f;
constexpr std::size_t kChatQueueLimit = 4096;
constexpr std::size_t kChatPumpLimit = 64;

struct CreateFence
{
	void *pendingMap;
	int pendingTurn;
	float sentTime;
};

struct NetEventHeapEntry
{
	float priority;
	int index;
};

struct NetEventHeap
{
	int count{};
	std::array<NetEventHeapEntry, MAX_SELECTED_EVENTS + 1> entries{};
};

struct NetEventCandidate
{
	int index;
	float score;
};

template <typename T> T &memory(std::uintptr_t rva)
{
	return *reinterpret_cast<T *>(g_base + rva);
}

template <typename T> T function(std::uintptr_t rva)
{
	return reinterpret_cast<T>(g_base + rva);
}

} // namespace

// Platform compatibility patches.
namespace
{
bool is_valid_platform_code(std::string_view code)
{
	return code == "pc" || code == "ps" || code == "xb" || code == "ns";
}

using RconManagerFn = std::uint64_t (*)(int, const wchar_t *, std::uint8_t, std::uint8_t);
using WriteCreateFn = void (*)(void *, void *);
using IsSendWindowOpenFn = bool (*)(int);
using NetObjStateMapAllocFn = void *(*)();
using NetObjStateMapFreeFn = void (*)(void *);
using JumpUsingEnergyFn = bool (*)(void *);
using RollUsingEnergyFn = bool (*)(void *);
using EndSprintFn = void (*)(void *);
using DropPlayerFn = void (*)(void *, int);
using SetNotPlayingFn = void (*)(int);
using VanishAllPlayersFn = void (*)();
using WeaponDispenserFireFn = void *(*)(void *);

RconManagerFn g_originalRconManager{};
WriteCreateFn g_originalWriteCreate{};
JumpUsingEnergyFn g_originalJumpUsingEnergy{};
DropPlayerFn g_originalDropPlayer{};
VanishAllPlayersFn g_originalVanishAllPlayers{};
WeaponDispenserFireFn g_originalWeaponDispenserFire{};
std::array<CreateFence, 64> g_createFences{};

} // namespace

void bf2server_patch_platform_lobby()
{
	const char *requested = std::getenv("PLATFORM_LOBBY");
	if (!requested)
	{
		Logger.log(LogLevel_INFO, "[platform-lobby] PLATFORM_LOBBY is unset; keeping the embedded lobby");
		return;
	}
	const std::string_view requestedCode(requested);
	if (!is_valid_platform_code(requestedCode))
	{
		Logger.log(LogLevel_ERROR, "[platform-lobby] invalid PLATFORM_LOBBY '%s'; expected pc, ps, xb, or ns",
				   requested);
		return;
	}

	auto *lobby = reinterpret_cast<std::uint8_t *>(g_base + OFFSET_PLATFORM_LOBBY);
	const auto size = *reinterpret_cast<const std::uint64_t *>(lobby + 0x10);
	const auto capacity = *reinterpret_cast<const std::uint64_t *>(lobby + 0x18);
	const std::string_view currentCode(reinterpret_cast<const char *>(lobby), 2);
	if (size != 2 || capacity != 15 || lobby[2] != 0 || !is_valid_platform_code(currentCode))
	{
		Logger.log(LogLevel_ERROR, "[platform-lobby] unexpected std::string state at Battlefront2.dll+0x%llX",
				   static_cast<unsigned long long>(OFFSET_PLATFORM_LOBBY));
		return;
	}

	const std::array<std::uint8_t, 2> current{lobby[0], lobby[1]};
	const std::array<std::uint8_t, 2> replacement{static_cast<std::uint8_t>(requestedCode[0]),
												  static_cast<std::uint8_t>(requestedCode[1])};
	if (current != replacement &&
		(memory<std::uint8_t>(OFFSET_PLATFORM_STATE_A) != 0 || memory<std::uint8_t>(OFFSET_PLATFORM_STATE_B) != 0))
	{
		Logger.log(LogLevel_ERROR,
				   "[platform-lobby] Photon already consumed '%c%c'; restart with earlier injection to select '%c%c'",
				   current[0], current[1], replacement[0], replacement[1]);
		return;
	}

	if (g_patcher->bytes(OFFSET_PLATFORM_LOBBY, replacement))
	{
		Logger.log(LogLevel_INFO, "[platform-lobby] selected '%c%c'", replacement[0], replacement[1]);
	}
}

// Security and stability patches.
void bf2server_patch_votekick_exploit()
{
	// 0x279F51: MOVZX EAX,[RSP+0x24] -> XOR EAX,EAX.
	g_patcher->bytes(OFFSET_VOTECRASH_FIX, {0x31, 0xC0, 0x90, 0x90, 0x90});
	// 0x23F0B6: Lua iVote reader call -> initialize its output local to zero.
	g_patcher->bytes(OFFSET_VOTEKICK_FIX, {0xC6, 0x45, 0xAB, 0x00, 0x90});
}

static bool map_gate_replacement()
{
	const auto calls = g_mapHangTicks.fetch_add(1, std::memory_order_relaxed) + 1;
	if (calls >= MAPFIX_IDLE_TIMEOUT)
	{
		// Match Galaxy's forced net-disabled path.
		return true;
	}

	if (memory<std::uint8_t>(OFFSET_NET_ENABLED) == 0) return true;
	using GetTimeFn = float (*)();
	using PredicateFn = bool (*)();
	const float now = function<GetTimeFn>(OFFSET_GET_TIME)();
	float &deadline = memory<float>(OFFSET_MAP_DEADLINE);
	if (deadline == memory<float>(OFFSET_MAP_DEADLINE_SENTINEL))
	{
		const bool host = function<PredicateFn>(OFFSET_HOST_INDEX_PREDICATE)();
		deadline = now + memory<float>(host ? OFFSET_HOST_DEADLINE_DELAY : OFFSET_CLIENT_DEADLINE_DELAY);
	}
	if (now > deadline) memory<std::uint8_t>(OFFSET_MAP_EXPIRED) = 1;
	return memory<std::uint8_t>(OFFSET_MAP_EXPIRED) != 0;
}

void bf2server_patch_maphang()
{
	// 0x269120: preserve the map deadline and force ready after 100 polls.
	g_patcher->replace(OFFSET_MAPFIX_DETOUR, 13, reinterpret_cast<void *>(&map_gate_replacement));
}

// Network replication patches.
void bf2server_patch_distance_lag()
{
	// 0x28DA86: MOV [RSP+...],5 -> 32 nearest player moves.
	g_patcher->bytes(OFFSET_DISTANCE_LAG, {0x20});
}

void bf2server_patch_waitlate_grace()
{
	// /waitlate accepts one preceding input turn instead of three.
	// 0x3CE7DD: grace immediate 3 -> 1; /nowaitlate remains 0.
	g_patcher->bytes(OFFSET_WAITLATE_GRACE, {0x01});
}

namespace
{
// EntityMine network-priority hooks.
// Reorder recurring state and CreateOrdnance candidates without bypassing
// either native packet budget.

bool is_entity_mine(const std::byte *object)
{
	return object && *reinterpret_cast<const std::uintptr_t *>(object) == g_base + OFFSET_ENTITY_MINE_VTABLE;
}

void prioritize_entity_mine_state(std::uintptr_t *returnAddress)
{
	// The sole caller's return address anchors the WriteObjects frame; its scored object list is at +0x2E0.
	if (!returnAddress || *returnAddress != g_base + OFFSET_WRITE_OBJECTS_BUDGET_RETURN) return;

	using ScopedObjectCountFn = int (*)();
	const int count = function<ScopedObjectCountFn>(OFFSET_SCOPED_OBJECT_COUNT)();
	if (count < 0 || count > MAX_SCOPED_OBJECTS) return;

	auto *callerStack = reinterpret_cast<std::byte *>(returnAddress + 1);
	auto **objects = reinterpret_cast<std::byte **>(callerStack + WRITE_OBJECT_LIST_STACK_OFFSET);
	std::array<std::byte *, MAX_SCOPED_OBJECTS> ordered{};
	int output = 0;
	for (int index = 0; index < count; ++index)
	{
		if (is_entity_mine(objects[index])) ordered[output++] = objects[index];
	}
	for (int index = 0; index < count; ++index)
	{
		if (!is_entity_mine(objects[index])) ordered[output++] = objects[index];
	}
	std::copy_n(ordered.begin(), count, objects);
}

void insert_net_event(NetEventHeap &heap, int index, float priority)
{
	if (heap.count >= MAX_SELECTED_EVENTS) return;

	int position = ++heap.count;
	while (position > 1)
	{
		const int parent = position >> 1;
		if (priority <= heap.entries[parent].priority) break;
		heap.entries[position] = heap.entries[parent];
		position = parent;
	}
	heap.entries[position] = {priority, index};
}

int pop_net_event(NetEventHeap &heap)
{
	const int result = heap.entries[1].index;
	const NetEventHeapEntry tail = heap.entries[heap.count--];
	if (heap.count == 0) return result;

	int position = 1;
	while ((position << 1) <= heap.count)
	{
		int child = position << 1;
		if (child < heap.count && heap.entries[child].priority < heap.entries[child + 1].priority) ++child;
		if (tail.priority >= heap.entries[child].priority) break;
		heap.entries[position] = heap.entries[child];
		position = child;
	}
	heap.entries[position] = tail;
	return result;
}

bool is_entity_mine_event(const std::byte *event)
{
	// Type zero is CreateOrdnance; native RTTI includes EntityMine subclasses.
	if (*reinterpret_cast<const std::uint32_t *>(event) != 0) return false;

	using FindOrdnanceClassFn = void *(*)(std::uint32_t);
	void *ordnanceClass = function<FindOrdnanceClassFn>(OFFSET_FIND_ORDNANCE_CLASS)(
		*reinterpret_cast<const std::uint8_t *>(event + ORDINARY_EVENT_CLASS));
	if (!ordnanceClass) return false;

	auto **vtable = *reinterpret_cast<void ***>(ordnanceClass);
	using IsRttiFn = bool (*)(void *, int);
	return reinterpret_cast<IsRttiFn>(vtable[3])(ordnanceClass, memory<int>(OFFSET_ENTITY_MINE_RTTI));
}

void send_net_events_replacement(void *packet)
{
	using ScoreNetEventFn = float (*)(const void *);
	using PacketByteCountFn = int (*)(const void *);
	using EventSectionLimitFn = int (*)(int);
	using WriteNetEventFn = void (*)(const void *, void *);
	using WritePacketBitFn = void (*)(void *, std::uint8_t);

	NetEventHeap heap{};
	std::array<NetEventCandidate, MAX_ORDINARY_EVENTS> mines{};
	std::array<NetEventCandidate, MAX_ORDINARY_EVENTS> ordinary{};
	int mineCount = 0;
	int ordinaryCount = 0;

	const int destination = memory<int>(OFFSET_CURRENT_DESTINATION);
	if (destination >= 0 && destination < 64)
	{
		auto *player = reinterpret_cast<std::byte *>(g_base + OFFSET_CURRENT_PLAYERS) +
					   static_cast<std::size_t>(destination) * NET_PLAYER_STRIDE;
		const int head = memory<int>(OFFSET_ORDINARY_EVENT_HEAD) & ORDINARY_EVENT_RING_MASK;
		int index = *reinterpret_cast<int *>(player + NET_PLAYER_EVENT_CURSOR) & ORDINARY_EVENT_RING_MASK;
		auto *ring = reinterpret_cast<std::byte *>(g_base + OFFSET_ORDINARY_EVENT_RING);
		auto scoreEvent = function<ScoreNetEventFn>(OFFSET_SCORE_NET_EVENT);

		// Scan the pending queue instead of Patch 3's newest-first 64-event subset.
		for (int scanned = 0; index != head && scanned < MAX_ORDINARY_EVENTS; ++scanned)
		{
			const std::byte *event = ring + static_cast<std::size_t>(index) * ORDINARY_EVENT_STRIDE;
			const float score = scoreEvent(event);
			if (score >= 0.0f)
			{
				const NetEventCandidate candidate{index, score};
				if (is_entity_mine_event(event))
				{
					mines[mineCount++] = candidate;
				}
				else
				{
					ordinary[ordinaryCount++] = candidate;
				}
			}
			index = (index + 1) & ORDINARY_EVENT_RING_MASK;
		}

		for (int candidate = 0; candidate < mineCount && heap.count < MAX_SELECTED_EVENTS; ++candidate)
		{
			insert_net_event(heap, mines[candidate].index, ENTITY_MINE_PRIORITY_BASE - mines[candidate].score);
		}
		for (int candidate = 0; candidate < ordinaryCount && heap.count < MAX_SELECTED_EVENTS; ++candidate)
		{
			insert_net_event(heap, ordinary[candidate].index, -ordinary[candidate].score);
		}

		// Native SendNetEvents consumes the queue snapshot even when the packet budget is exhausted.
		*reinterpret_cast<int *>(player + NET_PLAYER_EVENT_CURSOR) = head;

		auto packetBytes = function<PacketByteCountFn>(OFFSET_PACKET_BYTE_COUNT);
		auto writeEvent = function<WriteNetEventFn>(OFFSET_WRITE_NET_EVENT);
		const bool unbounded = memory<std::uint8_t>(OFFSET_UNBOUNDED_EVENTS) != 0;
		const int limit = memory<std::uint8_t>(OFFSET_HOST_COMM_PROBLEMS) != 0
							  ? 0
							  : function<EventSectionLimitFn>(OFFSET_EVENT_SECTION_LIMIT)(packetBytes(packet));
		while (heap.count != 0)
		{
			if (!unbounded && packetBytes(packet) >= limit) break;
			const int selected = pop_net_event(heap);
			writeEvent(ring + static_cast<std::size_t>(selected) * ORDINARY_EVENT_STRIDE, packet);
		}
	}

	// Preserve the native zero-bit event-list terminator.
	function<WritePacketBitFn>(OFFSET_WRITE_PACKET_BIT)(packet, 0);
}

__declspec(noinline) int object_budget_replacement()
{
	prioritize_entity_mine_state(reinterpret_cast<std::uintptr_t *>(_AddressOfReturnAddress()));

	const int destination = memory<int>(OFFSET_CURRENT_DESTINATION);
	const int size = memory<int>(OFFSET_NET_UPDATE_SIZE);
	int pending = 0;
	if (destination >= 0 && destination < 64)
	{
		auto *player = reinterpret_cast<std::byte *>(g_base + OFFSET_CURRENT_PLAYERS) +
					   static_cast<std::size_t>(destination) * 0x200;
		const int cursor = *reinterpret_cast<int *>(player + NET_PLAYER_EVENT_CURSOR);
		pending = (memory<int>(OFFSET_ORDINARY_EVENT_HEAD) - cursor) & ORDINARY_EVENT_RING_MASK;
	}
	const int reserve = std::min(std::max(pending - 3, 0) * 32, 200);
	// Preserve Classic's 1700/2048 baseline and reserve object space only for pending NetEvents.
	const int scale = 1700 - reserve;
	return static_cast<int>((static_cast<std::int64_t>(size) * scale) / 2048);
}

} // namespace

// Install both EntityMine priority paths.
// Persistent object state and one-shot creates retain their separate budgets.
void bf2server_patch_object_budget()
{
	// 0x3CE750: replace the recurring-state budget helper.
	// Stable-partition EntityMine records and reserve up to 200 scale units.
	g_patcher->replace(OFFSET_OBJECT_BUDGET, 17, reinterpret_cast<void *>(&object_budget_replacement));

	// 0x283690: replace the native 64-event score/heap scan.
	// Scan all 512 events and insert eligible EntityMine creates first.
	g_patcher->replace(OFFSET_SEND_NET_EVENTS, 12, reinterpret_cast<void *>(&send_net_events_replacement));
}

namespace
{
// 30 UPS send-scheduling hooks.
// Track CREATE transactions, guard acknowledgement slots, and gate interim updates.

void clear_create_fence(CreateFence &fence)
{
	fence = {};
}

// Hold a destination while map B still identifies the emitted CREATE turn.
// Native ACK/NACK handling replaces or resets B; timeout repeats the NACK
// reset so the CREATE can be regenerated without an intervening update.
bool create_fence_blocks(int client)
{
	if (client < 0 || client >= static_cast<int>(g_createFences.size())) return false;

	auto &fence = g_createFences[client];
	if (!fence.pendingMap) return false;

	auto *player = reinterpret_cast<std::byte *>(g_base + OFFSET_CURRENT_PLAYERS) +
				   static_cast<std::size_t>(client) * NET_PLAYER_STRIDE;
	void *pendingMap = *reinterpret_cast<void **>(player + NET_PLAYER_PENDING_MAP);
	if (pendingMap != fence.pendingMap ||
		*reinterpret_cast<int *>(static_cast<std::byte *>(pendingMap) + NETOBJ_MAP_TURN) != fence.pendingTurn)
	{
		clear_create_fence(fence);
		return false;
	}

	float retryTime =
		*reinterpret_cast<float *>(player + NET_PLAYER_SRTT) + *reinterpret_cast<float *>(player + NET_PLAYER_RTTVAR);
	if (!(retryTime >= CREATE_RETRY_FLOOR)) retryTime = CREATE_RETRY_FLOOR;

	using GetTimeFn = float (*)();
	const float elapsed = function<GetTimeFn>(OFFSET_GET_TIME)() - fence.sentTime;
	if (!(elapsed > retryTime)) return true;

	// ReadSwitchResponses result 2 frees B and installs a fresh pending map.
	function<NetObjStateMapFreeFn>(OFFSET_NETOBJ_MAP_FREE)(pendingMap);
	*reinterpret_cast<void **>(player + NET_PLAYER_PENDING_MAP) =
		function<NetObjStateMapAllocFn>(OFFSET_NETOBJ_MAP_ALLOC)();
	clear_create_fence(fence);
	return false;
}

bool send_window_gate_hook(int client)
{
	// Retain native slot timeout/congestion maintenance, then apply the CREATE fence.
	function<IsSendWindowOpenFn>(OFFSET_IS_SEND_WINDOW_OPEN)(client);
	return !create_fence_blocks(client);
}

void write_create_hook(void *packet, void *object)
{
	g_originalWriteCreate(packet, object);

	const int client = memory<int>(OFFSET_CURRENT_DESTINATION);
	if (client < 0 || client >= static_cast<int>(g_createFences.size())) return;

	auto *player = reinterpret_cast<std::byte *>(g_base + OFFSET_CURRENT_PLAYERS) +
				   static_cast<std::size_t>(client) * NET_PLAYER_STRIDE;
	void *pendingMap = *reinterpret_cast<void **>(player + NET_PLAYER_PENDING_MAP);
	if (pendingMap)
	{
		using GetTimeFn = float (*)();
		g_createFences[client] = {pendingMap,
								  *reinterpret_cast<int *>(static_cast<std::byte *>(pendingMap) + NETOBJ_MAP_TURN),
								  function<GetTimeFn>(OFFSET_GET_TIME)()};
	}
}

// Install one-turn scheduling with a per-destination CREATE transaction fence.
// IsPipeFull and native switch-response parsing remain unchanged; only the
// old interval-only CREATE delay and IsSendWindowOpen branch are replaced.
void bf2server_patch_send_scheduling()
{
	std::fill(g_createFences.begin(), g_createFences.end(), CreateFence{});

	// 0x289C20: record map B after writing a common object CREATE.
	// The tracked pointer and turn distinguish this transaction from later maps.
	const bool createInstalled = g_patcher->detour(OFFSET_WRITE_CREATE, 5, reinterpret_cast<void *>(&write_create_hook),
												   reinterpret_cast<void **>(&g_originalWriteCreate));
	if (!createInstalled) return;

	// 0x284B7F: JNC unsafe slot write -> JNC native pacing tail.
	// Prevent SentUpdate from indexing beyond its two acknowledgement slots.
	const bool slotInstalled = g_patcher->bytes(OFFSET_SEND_UPDATE2_SLOT_BRANCH, {0x73, 0x53});
	if (!slotInstalled) return;

	// 0x283FB3: CALL IsSendWindowOpen -> CALL send_window_gate_hook.
	// Preserve native maintenance and block interim updates until CREATE resolves.
	const bool gateInstalled =
		g_patcher->call(OFFSET_SEND_WINDOW_CALL, reinterpret_cast<void *>(&send_window_gate_hook));
	if (!gateInstalled) return;

	// 0x284C6B: ADD ECX,EAX -> INC ECX.
	// Keep unfenced destinations eligible again on the following server turn.
	g_patcher->bytes(OFFSET_SEND_UPDATE2_DELAY, {0xFF, 0xC1});
}

} // namespace

// 30 UPS network-update patch set.
// Visit all clients and use the CREATE-aware scheduler while retaining the
// native IsPipeFull capacity check and Classic's render/window pacing.
void bf2server_patch_netupdate()
{
	// 0x283E2E: remove signed /2 from the netCurMaxPlayers send budget.
	g_patcher->bytes(OFFSET_UPS_CLIENT_LIMITER, {0x90, 0x90, 0x90, 0x90, 0x90});

	// Apply one-turn scheduling with acknowledgement fencing for CREATE updates.
	bf2server_patch_send_scheduling();

	// 0x283FA8: retain the stock IsPipeFull skip.
}

// Weapons and movement patches.
static void *weapon_dispenser_fire_hook(void *weaponPointer)
{
	auto *weapon = static_cast<std::byte *>(weaponPointer);
	float &strength = *reinterpret_cast<float *>(weapon + WEAPON_DISPENSER_STRENGTH);
	if (!std::isfinite(strength) || strength < 0.0f)
		strength = 0.0f;
	else if (strength > 1.0f)
		strength = 1.0f;
	return g_originalWeaponDispenserFire(weaponPointer);
}

void bf2server_patch_speedpacks()
{
	// Clamp Weapon+0x184 before every server-side dispenser launch.
	// 0x3681E0-0x36822C: detour WeaponDispenser::Fire and preserve its prologue.
	g_patcher->detour(OFFSET_WEAPON_DISPENSER_FIRE, 0x4D, reinterpret_cast<void *>(&weapon_dispenser_fire_hook),
					  reinterpret_cast<void **>(&g_originalWeaponDispenserFire));
}

static bool jump_using_energy_hook(void *soldierPointer)
{
	auto *soldier = static_cast<std::byte *>(soldierPointer);
	auto *soldierClass = *reinterpret_cast<std::byte **>(soldier + 0x668);
	if (!soldierClass) return g_originalJumpUsingEnergy(soldierPointer);

	const auto *velocity = reinterpret_cast<const float *>(soldier + 0x738);
	const auto *forward = reinterpret_cast<const float *>(soldier + 0x1A0);
	const float projection = velocity[0] * forward[0] + velocity[1] * forward[1] + velocity[2] * forward[2];
	const float threshold = *reinterpret_cast<float *>(soldierClass + 0x9BC) + memory<float>(OFFSET_JUMP_EPSILON);
	const float normalCost = *reinterpret_cast<float *>(soldierClass + 0xAF4);
	const float sprintCost = *reinterpret_cast<float *>(soldierClass + 0xAF8);
	const auto energyFlags = *reinterpret_cast<std::uint32_t *>(soldier + SOLDIER_ENERGY_FLAGS);

	if ((energyFlags & 1U) != 0 && normalCost <= 0.0f && sprintCost > normalCost && projection > threshold &&
		projection <= threshold + 0.1f)
	{
		auto *controllable = soldier + SOLDIER_CONTROLLABLE;
		auto **vtable = *reinterpret_cast<void ***>(controllable);
		using ApplyJumpFn = bool (*)(void *);
		return reinterpret_cast<ApplyJumpFn>(vtable[0x150 / sizeof(void *)])(controllable);
	}
	return g_originalJumpUsingEnergy(soldierPointer);
}

void bf2server_patch_locked_jump()
{
	// Apply the low-stamina fallback at the shared JumpUsingEnergy entry.
	// 0x172210: detour the helper used by both Classic jump paths.
	g_patcher->detour(OFFSET_LOCKED_JUMP, 5, reinterpret_cast<void *>(&jump_using_energy_hook),
					  reinterpret_cast<void **>(&g_originalJumpUsingEnergy));
}

static bool sprint_roll_hook(void *soldierPointer)
{
	auto *soldier = static_cast<std::byte *>(soldierPointer);
	const bool rolled = function<RollUsingEnergyFn>(OFFSET_ROLL_USING_ENERGY)(soldierPointer);
	if (!rolled && (*reinterpret_cast<std::uint32_t *>(soldier + SOLDIER_ENERGY_FLAGS) & 1U) != 0)
	{
		auto *controllable = soldier + SOLDIER_CONTROLLABLE;
		auto **vtable = *reinterpret_cast<void ***>(controllable);
		reinterpret_cast<EndSprintFn>(vtable[END_SPRINT_VTABLE / sizeof(void *)])(controllable);
	}
	return rolled;
}

void bf2server_patch_infinite_sprint()
{
	// Clear sprint state when its RollUsingEnergy attempt is rejected.
	// 0x18323E: redirect only the sprint-state helper call.
	g_patcher->call(OFFSET_SPRINT_ROLL_CALL, reinterpret_cast<void *>(&sprint_roll_hook));
}

// Player lifecycle and spawning patches.
static void drop_player_hook(void *hostGame, int player)
{
	// Clear active membership before preserving Classic's normal shell cleanup.
	function<SetNotPlayingFn>(OFFSET_SET_NOT_PLAYING)(player);
	g_originalDropPlayer(hostGame, player);
}

void bf2server_patch_preplay_disconnect()
{
	// Remove abandoned players from next-playing membership before a match.
	// 0x2924B0: wrap DropPlayer and retain its normal shell cleanup.
	g_patcher->detour(OFFSET_PREPLAY_DISCONNECT, 9, reinterpret_cast<void *>(&drop_player_hook),
					  reinterpret_cast<void **>(&g_originalDropPlayer));
}

static int spawn_gate_replacement(int player) noexcept
{
	const int maxPlayers = memory<int>(OFFSET_NET_CUR_MAX_PLAYERS);
	if (player < 0 || player >= maxPlayers || player >= 64) return -1;

	const std::uint64_t playingMask = memory<std::uint64_t>(OFFSET_PLAYING_MASK);
	if ((playingMask & (std::uint64_t{1} << player)) == 0) return -1;

	auto *players = reinterpret_cast<std::byte *>(g_base + OFFSET_CURRENT_PLAYERS);
	auto *current = players + static_cast<std::size_t>(player) * NET_PLAYER_STRIDE;
	const int currentTicket = *reinterpret_cast<int *>(current + NET_PLAYER_SPAWN_TICKET);
	if (currentTicket <= 0) return currentTicket;

	const auto currentTeam = *reinterpret_cast<std::uint8_t *>(current + NET_PLAYER_TEAM);
	const int scanCount = std::min(maxPlayers, 64);
	for (int candidate = 0; candidate < scanCount; ++candidate)
	{
		if ((playingMask & (std::uint64_t{1} << candidate)) == 0) continue;
		auto *other = players + static_cast<std::size_t>(candidate) * NET_PLAYER_STRIDE;
		if (*reinterpret_cast<std::uint8_t *>(other + NET_PLAYER_TEAM) != currentTeam) continue;
		const int otherTicket = *reinterpret_cast<int *>(other + NET_PLAYER_SPAWN_TICKET);
		if (otherTicket >= 0 && otherTicket < currentTicket) return currentTicket;
	}
	return 0;
}

void bf2server_patch_spawnbug()
{
	// Use the 0x271EE3 INT3 pad as a relay and redirect only the respawn-gate caller.
	const bool relayInstalled =
		g_patcher->replace(OFFSET_SPAWN_GATE_RELAY, 12, reinterpret_cast<void *>(&spawn_gate_replacement));
	if (!relayInstalled) return;

	g_patcher->bytes(OFFSET_SPAWN_GATE_CALL, {0xE8, 0xA5, 0x16, 0x1B, 0x00});
}

static std::uint64_t set_spawn_delay_replacement()
{
	using LuaToNumberFn = double (*)(void *, int);
	using SetSpawnDelayFn = void (*)(void *, float, float, unsigned int);
	void *lua = memory<void *>(OFFSET_LUA_STATE);
	const float requestedDelay = static_cast<float>(function<LuaToNumberFn>(OFFSET_LUA_TO_NUMBER)(lua, 1));
	const float slotDelay = static_cast<float>(function<LuaToNumberFn>(OFFSET_LUA_TO_NUMBER)(lua, 2));
	bool networked = memory<std::uint8_t>(OFFSET_NET_ENABLED) != 0;
	if (memory<std::uint8_t>(OFFSET_IDLE) != 0)
	{
		networked = memory<std::uint8_t>(OFFSET_STAGED_NET_ENABLED) != 0;
	}
	const float delay = networked ? g_spawnValue : requestedDelay;
	void *manager = memory<void *>(OFFSET_SPAWN_MANAGER);
	for (unsigned int team = 1; team <= 2; ++team)
	{
		function<SetSpawnDelayFn>(OFFSET_SET_SPAWN_DELAY)(manager, delay, slotDelay, team);
	}
	return 0;
}

void bf2server_patch_spawnvalue()
{
	char *envBuffer = nullptr;
	size_t envSize = 0;
	if (_dupenv_s(&envBuffer, &envSize, "SPAWN_TIMER") == 0 && envBuffer != nullptr)
	{
		char *end{};
		const float parsed = std::strtof(envBuffer, &end);
		if (end != envBuffer && std::isfinite(parsed) && parsed >= 0.0f)
		{
			g_spawnValue = parsed;
		}
	}
	free(envBuffer);

	// Configure the normal respawn-wave delay from SPAWN_TIMER.
	// 0x22F000: replace the callback that forces 15.0f while networked.
	g_patcher->replace(OFFSET_SPAWNVALUE_CALLBACK, 13, reinterpret_cast<void *>(&set_spawn_delay_replacement));
}

// Warmup-to-play spawn transition.
// Vanish players, reset each team timer, and require active characters to
// enter the wave after the transition instead of jump-spawning at zero.
static void vanish_all_players_hook()
{
	g_originalVanishAllPlayers();
	auto *manager = static_cast<std::byte *>(memory<void *>(OFFSET_SPAWN_MANAGER));
	if (!manager) return;

	for (int team = 0; team < 8; ++team)
	{
		const auto offset = static_cast<std::size_t>(team) * sizeof(float);
		*reinterpret_cast<float *>(manager + 0xB4 + offset) = *reinterpret_cast<float *>(manager + 0x74 + offset);
		*reinterpret_cast<float *>(manager + 0xF4 + offset) = *reinterpret_cast<float *>(manager + 0x94 + offset);
	}

	using IsPlayingFn = bool (*)(int, bool);
	using FindCharacterFn = std::byte *(*)(int);
	int maxPlayers = memory<int>(OFFSET_NET_CUR_MAX_PLAYERS);
	if (maxPlayers > 64) maxPlayers = 64;
	for (int player = 0; player < maxPlayers; ++player)
	{
		if (!function<IsPlayingFn>(OFFSET_IS_PLAYING)(player, false)) continue;
		std::byte *character = function<FindCharacterFn>(OFFSET_FIND_CHARACTER)(player);
		if (!character) continue;
		const int team = *reinterpret_cast<int *>(character + 0x164);
		if (team < 0 || team >= 8) continue;
		const int wave = *reinterpret_cast<int *>(manager + 0xD4 + static_cast<std::size_t>(team) * sizeof(int));
		*reinterpret_cast<int *>(character + 0x1A8) = wave + 1;
	}
}

void bf2server_patch_pregame_spawn()
{
	// Apply the timer and required-wave reset during the native transition.
	// 0x288770: wrap VanishAllPlayers and update the expanded Classic fields.
	const bool stateInstalled =
		g_patcher->detour(OFFSET_PREGAME_VANISH, 12, reinterpret_cast<void *>(&vanish_all_players_hook),
						  reinterpret_cast<void **>(&g_originalVanishAllPlayers));
	if (!stateInstalled) return;

	// End pregame at equality instead of leaving one zero-time iteration.
	// 0x288447: JLE -> JL.
	g_patcher->bytes(OFFSET_PREGAME_END_BRANCH, {0x7C});
}

// RCON and game integration.
namespace
{
std::string narrow(const wchar_t *text)
{
	if (!text) return {};
	const int needed = WideCharToMultiByte(CP_ACP, 0, text, -1, nullptr, 0, nullptr, nullptr);
	if (needed <= 1) return {};
	std::string result(static_cast<std::size_t>(needed), '\0');
	if (WideCharToMultiByte(CP_ACP, 0, text, -1, result.data(), needed, nullptr, nullptr) != needed) return {};
	result.pop_back();
	return result;
}

void queue_chat(int player, const wchar_t *message)
{
	using PlayerNameFn = const char *(*)(int);
	const char *name = "None";
	if (player == 0x40)
	{
		name = "admin";
	}
	else if (player >= 0)
	{
		if (const char *candidate = function<PlayerNameFn>(OFFSET_PLAYER_NAME)(player))
		{
			name = candidate;
		}
	}
	const std::string body = narrow(message);
	std::string line;
	line.reserve(std::strlen(name) + body.size() + 2);
	line.push_back('\t');
	line.append(name);
	line.push_back('\t');
	line.append(body);
	std::scoped_lock lock(g_chatMutex);
	if (!g_chatCallback) return;
	if (g_chatQueue.size() >= kChatQueueLimit) g_chatQueue.pop_front();
	g_chatQueue.emplace_back(std::move(line));
}

std::uint64_t rcon_manager_hook(int player, const wchar_t *message, std::uint8_t sender, std::uint8_t messageType)
{
	const auto result = g_originalRconManager(player, message, sender, messageType);
	if (messageType != 0 && message) queue_chat(player, message);
	return result;
}

} // namespace

void bf2server_set_chat_cc()
{
	// 0x25C7E0: wrap the native RCON/chat manager and queue chat for the worker.
	g_patcher->detour(OFFSET_CHATINPUT, 12, reinterpret_cast<void *>(&rcon_manager_hook),
					  reinterpret_cast<void **>(&g_originalRconManager));
}

std::string bf2server_command(DWORD messageType, DWORD sender, const wchar_t *message, DWORD responseOutput)
{
	std::scoped_lock lock(g_commandMutex);
	if (!g_originalRconManager) return "RCON bridge unavailable";

	auto &loggedIn = memory<std::uint8_t>(OFFSET_LOGGED_IN);
	auto &details = memory<std::uint8_t>(OFFSET_COMMAND_DETAILS);
	const auto oldLoggedIn = loggedIn;
	const auto oldDetails = details;
	loggedIn = 1;
	details = 1;
	g_originalRconManager(static_cast<int>(responseOutput), message, static_cast<std::uint8_t>(sender),
						  static_cast<std::uint8_t>(messageType));
	loggedIn = oldLoggedIn;
	details = oldDetails;
	return std::string(reinterpret_cast<char *>(g_base + OFFSET_RESBUFFER));
}

std::string bf2server_get_adminpwd()
{
	return std::string(reinterpret_cast<char *>(g_base + OFFSET_ADMINPW));
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
	std::scoped_lock lock(g_chatMutex);
	g_chatCallback = std::move(onChat);
	if (!g_chatCallback) g_chatQueue.clear();
}

bool bf2server_pump_chat()
{
	std::deque<std::string> pending;
	std::function<void(std::string const &)> callback;
	bool empty;
	{
		std::scoped_lock lock(g_chatMutex);
		if (!g_chatCallback)
		{
			g_chatQueue.clear();
			return true;
		}
		std::size_t count = std::min(g_chatQueue.size(), kChatPumpLimit);
		while (count-- > 0)
		{
			pending.emplace_back(std::move(g_chatQueue.front()));
			g_chatQueue.pop_front();
		}
		empty = g_chatQueue.empty();
		callback = g_chatCallback;
	}
	for (const std::string &message : pending) callback(message);
	return empty;
}

USHORT bf2server_get_gameport()
{
	return memory<USHORT>(OFFSET_GAMEPORT);
}

MapStatus bf2server_get_map_status()
{
	return static_cast<MapStatus>(memory<std::uint8_t>(OFFSET_MAP_STATUS));
}

bool bf2server_idle()
{
	return memory<std::uint8_t>(OFFSET_IDLE) == 1;
}

void bf2server_mapfix_tick()
{
	if (bf2server_get_map_status() == MAP_IDLE)
	{
		g_mapHangTicks.store(0, std::memory_order_relaxed);
	}
}

int bf2server_lua_dostring(std::string const &code)
{
	using ExecuteFn = int (*)(void *, const char *, std::size_t, const char *);
	void *lua = memory<void *>(OFFSET_LUA_STATE);
	if (!lua) return -1;
	return function<ExecuteFn>(OFFSET_LUA_EXECUTE)(lua, code.data(), code.size(), "=rcon");
}

// Patch installation order.
void bf2server_init()
{
	g_module = GetModuleHandleW(L"Battlefront2.dll");
	if (!g_module)
	{
		Logger.log(LogLevel_ERROR, "Battlefront2.dll is not loaded");
		return;
	}
	g_base = reinterpret_cast<std::uintptr_t>(g_module);

	g_patcher = std::make_unique<PatchEngine>(g_module);
	Logger.log(LogLevel_VERBOSE, "Patching BattlefrontII process...");

	bf2server_patch_platform_lobby();

	bf2server_patch_votekick_exploit();
	bf2server_patch_maphang();

	bf2server_patch_distance_lag();
	bf2server_patch_waitlate_grace();
	bf2server_patch_object_budget();
	// bf2server_patch_netupdate();

	bf2server_patch_speedpacks();
	bf2server_patch_locked_jump();
	bf2server_patch_infinite_sprint();
	bf2server_patch_preplay_disconnect();

	bf2server_patch_spawnbug();
	bf2server_patch_spawnvalue();
	bf2server_patch_pregame_spawn();

	bf2server_set_chat_cc();
	Logger.log(LogLevel_VERBOSE, "All patches applied.");
}
