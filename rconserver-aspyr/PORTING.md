# rconserver-galaxy to Classic Collection port record

## Scope and target

This port targets the x64 `Battlefront2.dll` used by the current Classic
Collection Patch 3. Addresses below are RVAs from the module base. The primary
oracle was Ghidra Master program `/Battlefront2_CC.dll`; GOG behavior was checked
against `/BattlefrontII.exe`, and source-level names/intent were checked against
Ghidra Reference `/BattlefrontII_Server.exe`.

The port is semantic, not an address translation. Each Galaxy patch was first
traced to the state transition it changes. The Classic implementation was then
identified independently and its exact instruction bytes recorded. Literal
ports were rejected where the underlying subsystem no longer exists or where
Classic's operational evidence makes the Galaxy edit harmful.

The analyzed Patch 3 image and the live on-disk image differ at the distance-lag
immediate because the live DLL already contains the user's `5 -> 32` patch. The
runtime writes the supported Patch 3 RVAs directly and performs no whole-file
hash or patch-preimage validation.

## Original Galaxy harness

The Galaxy project builds a 32-bit MSVC DLL named `RconServer_32.dll`. Its
original control flow is:

1. `DllMain` calls `bf2server_init`, then creates a worker thread.
2. The worker binds TCP on the numeric UDP game port and starts an accept thread.
3. Each accepted client gets a detached command thread.
4. The login packet is 32 lowercase ASCII MD5 characters followed by byte
   `0x64`; the server returns one authorization byte.
5. A request is one row-count byte, one size byte, then a NUL-terminated command.
6. A response is a row-count byte followed by `(length, NUL-terminated row)`
   records. Chat and `Game has ended` use the same unsolicited row format.
7. `/lua ` is handled inside the DLL. Other commands set the native logged-in and
   details flags, invoke the game's RCON parser with output destination `-1`, and
   copy the native response buffer.

The Classic harness preserves that protocol. Its implementation changes are
engineering fixes, not protocol changes:

- `DllMain` only disables thread notifications and starts one bootstrap thread;
- all partial `recv`/`send` operations are completed in loops;
- native chat is copied into a bounded queue and broadcast off the game thread;
- RCON command execution is serialized;
- authentication uses the same MD5 source implementation as Galaxy;
- x64 entry detours use documented instruction boundaries and relay/trampoline
  allocation rather than x86 naked inline assembly.

## Cross-project maintenance structure

The Aspyr source tree now mirrors the Galaxy harness at the patch-maintenance
boundaries. All Patch 3 RVAs are centralized in `bf2server.h` using the same
`OFFSET_*` naming style. Every gameplay change has a public, individually named
`bf2server_patch_*` function, and `bf2server_init` is an explicit list of those
functions in Galaxy order. `RconClient` again lives in `RconClient.h/.cpp`, while
socket ownership and chat fanout remain in `RconServer.h/.cpp`. Common helpers
retain Galaxy names and signatures: `bf2server_command`,
`bf2server_get_adminpwd`, `bf2server_s2ws`, `bf2server_set_chat_cb`,
`bf2server_pump_chat`, `bf2server_get_gameport`, `bf2server_get_map_status`,
`bf2server_idle`, `bf2server_mapfix_tick`, and `bf2server_lua_dostring`.

The remaining structural differences are intentional:

- `PatchEngine.*` replaces x86 naked assembly with protected x64 writes, near
  relays, and trampolines;
- Classic detours the native RCON/chat manager while Galaxy redirects its
  `snprintf` operand; both queue chat for `bf2server_pump_chat` on the worker;
- patch installation remains on the bootstrap worker rather than under the
  Windows loader lock in `DllMain`.

## Patch inventory and disposition

| Galaxy feature | GOG site/code | Classic result | Port status |
|---|---|---|---|
| `/norender` compatibility | `0x6BB37F`, bypass failed render-object construction | Classic has a different headless path whose pacing is unstable | Excluded; SWBF2Admin removes `/norender` from Aspyr startup instead |
| vote request exploit | `ReadBootRequest 0x5D2282`, force decoded byte zero | same decode/store at `+0x279F51` | Ported |
| disable Lua `iVote` | call in `ScriptCB_SetNetGameDefaults 0x599B11` | same registered callback `+0x23F040`, call at `+0x23F0B6` | Ported; initializes the output local to zero instead of leaving it undefined |
| password workaround | replaces `PasswordStr` local-buffer reader at `0x599BC5` with global plaintext buffer | Classic launch parser `+0xEAD00` copies `/password` and directly calls `SetPassword +0x296F60` | Excluded; it is unnecessary on Classic |
| dedicated query metadata | QR2/GameSpy `fgr_int_r0` at `0x5D7F31`, `fgd_int_serverType` at `0x5D800F` | both strings and the QR2 response path are absent; Classic publishes Photon/RedNet metadata | Not applicable |
| map-hang watchdog | gate `0x5B6076`, after 100 endgame polls take network-disabled/ready path | equivalent gate `+0x269120`, map state `+0xAEFF90` | Ported as an exact whole-function semantic replacement |
| distance relay | `WritePlayerMoves 0x5D38B8`, nearest count `5 -> 32` | `WritePlayerMoves +0x28D8A0`, immediate `+0x28DA86` | Ported |
| `/waitlate` grace | helper immediate at `0x5BAAD0`, `3 -> 1`; nowaitlate remains zero | helper `+0x3CE7D0`, immediate `+0x3CE7DD` | Ported |
| event-aware object budget and EntityMine priority | cave at `0x5CE71C`; reserve 32 scale units/event after three, clamp 200; stable-partition EntityMine state and prioritize its CreateOrdnance events | budget helper `+0x3CE750`; `SendNetEvents +0x283690`; ordinary ring cursor `NetPlayer+0x3C`, head `+0x9EA89C` | Ported with Classic's 1700/2048 base scale and 512-entry ring |
| locked jump | calls `JumpUsingEnergy +0x4EDC60` at `0x4EAEA2/0x4EB15C` | folded single call to `+0x172210` at `+0x182E2B` | Ported by wrapping the sole helper xref |
| infinite sprint | sprint roll call at `0x4EB146` ignores a failed `RollUsingEnergy +0x4EDD30` result | same ignored result at `+0x18323E`, helper `+0x17A8E0` | Ported by ending sprint only on a failed roll with exhausted Energy |
| pre-play disconnect | replace two shell drop calls with `SetNotPlaying +0x5B9440` | `DropPlayer +0x2924B0`, `SetNotPlaying +0x285E30` | Ported; clears endpoint membership and retains Classic shell cleanup |
| configurable spawn delay | redirects stock `15.0f` operand at `0x58D609` to DLL storage | callback `+0x22F000` forces `15.0f` only when networked | Ported as a full callback replacement using `SPAWN_TIMER` |
| pregame spawn transition | `UpdatePreGame 0x5C4EF0`: `JLE -> JL`, wrap Vanish call | `UpdatePreGame +0x288400`: branch `+0x288447`, Vanish `+0x288770` | Ported with expanded Classic fields |
| update scheduling | render/sleep, `/2` budget, slot guard, per-client CREATE acknowledgement fence | `SendToClients +0x283E10`, `SendUpdate2 +0x284B40`, CREATE writer `+0x289C20` | Network scheduling ported; render/sleep edits intentionally omitted |
| RCON/chat/Lua bridge | parser `0x5B0030`, response/admin globals, `snprintf` operand hook, Lua C API | parser `+0x25C7E0`, response/admin globals, entry chat hook, `+0x3863A0` Lua wrapper | Ported |

## Classic-only runtime ports

### Spawn-queue disconnect hole

The native raw-ticket helper at `+0x271E90` returns `NetPlayer+0x158` after
bounds and playing checks. Its call at `+0xC0839` is followed by `TEST EAX,EAX`
and rejects every nonzero ticket, while the independent call at `+0x98610`
formats `ticket+1` for the spawn-screen HUD. Queue insertion at `+0x268200`
assigns one above the largest live same-team ticket, and dequeue at `+0x281810`
decrements larger same-team tickets. `NetGame::RemovePlayer +0x281980` does not
perform that compaction, so disconnecting ticket zero can leave only positive
live tickets and permanently block the team.

`bf2server_patch_spawnbug` preserves the HUD caller and redirects only the gate
call to a replacement that accepts the smallest nonnegative ticket held by a
playing teammate. The call targets a 12-byte absolute-jump relay installed in
the 13-byte `INT3` pad at `+0x271EE3`; the replacement itself lives in
`RconServer_64.dll`. It scans at most 64 players because the authoritative
playing mask at `+0x9E0228` is 64 bits.

### Platform lobby

The object at `+0x64AA58` is an MSVC small-string object whose inline contents
are `pc`, size is two, and capacity is fifteen. Photon setup paths `+0x40B500`
and `+0x40BB70` obtain it through the getter at `+0x3EA890` and append it to
`bf1-` or `bf2-`. `bf2server_patch_platform_lobby` validates that complete
object state, accepts only `pc`, `ps`, `xb`, or `ns` from `PLATFORM_LOBBY`, and
writes the two inline characters without crossing CRT/STL ownership boundaries.
It refuses a differing late change after the setup state bytes at `+0xF83B40`
or `+0xF83B41` become nonzero.

Both former ProcessWriter definitions were removed. `RconServer_64` installs
the spawn-queue fix first, followed by platform selection, so there is only one
owner for their patch sites.

## Classic disassembly evidence

### RCON and chat

`RconManager +0x25C7E0` has the Win64 arguments `(int player,
const wchar_t* message, byte sender, byte messageType)`. Command output uses:

- admin password `+0x9C13A0`;
- details `+0x9C13FE`;
- global logged-in override `+0x9C13FF`;
- response buffer `+0x9C1450`;
- player-name getter `+0x271620`.

When `messageType != 0`, native code converts the message and formats
`"\t%s\t%s"` before sending it to its five native RCON sockets. The hook calls
the original parser first and queues the same name/message representation for the
SWBF2Admin clients. The verified 12-byte prologue is:

```asm
25C7E0  44 88 44 24 18       mov [rsp+18],r8b
25C7E5  89 4C 24 08          mov [rsp+08],ecx
25C7E9  55                   push rbp
25C7EA  53                   push rbx
25C7EB  56                   push rsi
```

The Lua bridge reads the state pointer at `+0x9AFB38` and invokes
`+0x3863A0(lua, buffer, length, "=rcon")`, the existing load-and-protected-call
wrapper used by native script callers.

### Send selection and window behavior

`SendToClients +0x283E10` computes only half of `netCurMaxPlayers`:

```asm
283E28  mov eax,[netCurMaxPlayers]
283E2E  cdq
283E2F  sub eax,edx
283E31  sar eax,1
```

The port NOPs the five-byte signed divide sequence. The function still scans for
the oldest eligible destination and retains `IsPipeFull`. Its sole call to
`IsSendWindowOpen +0x276710` at `+0x283FB3` is redirected to a wrapper. The
wrapper invokes the native function for slot timeout and congestion maintenance,
then admits the destination unless its DLL-side CREATE fence remains active.

### Two-slot overflow and CREATE-aware cadence

Classic `SendUpdate2 +0x284B40` scans two outstanding-update slots at
`NetPlayer+0x1AC/+0x1B0`. Stock code falls through with index 2 when both are
occupied and writes:

```asm
284BAE  mov edx,[hostTurn]
284BB4  mov [player + index*4 + 1ACh],edx
284BCB  movss [player + index*4 + 1B4h],xmm0
```

Index 2 aliases the following fields. At `+0x284B7F`, the full-slot `JNC` is
retargeted from the unsafe write block to the native pacing tail at `+0x284BD4`.
The stock function therefore still completes its cadence work without indexing
past the two physical slots.

`WriteUpdate +0x28E910` owns two object-state maps at `NetPlayer+0x0` (A) and
`NetPlayer+0x8` (B). B carries its update turn at `map+0x208`. Object creation is
proven at `WriteObjects +0x28CF18 -> WriteCreate +0x289C20`; the writer hook
records the destination's exact B pointer, turn, and send time after serializing
a CREATE.

`ReadSwitchResponses +0x27E100` resolves that transaction. ACK result 1 frees A,
promotes B to A, and allocates a fresh B. NACK result 2 frees B and allocates a
fresh B. The fence therefore clears as soon as either the pointer or turn stamp
changes. Until then the scheduler skips every ordinary update for that client,
preventing an interim update from replacing the CREATE transaction.

If no response arrives within `max(SRTT + RTTVAR, 0.1 seconds)`, the wrapper
performs the native NACK reset with `NetObjStateMap::Free +0x26F370` and
`NetObjStateMap::Alloc +0x2685A0`. The CREATE can then be regenerated instead of
holding the client indefinitely. At `SendUpdate2 +0x284C6B`, `ADD ECX,EAX`
becomes `INC ECX`, making every unfenced destination eligible on the next host
turn. This is the x64 semantic equivalent of Galaxy's frame-specific caves.

### Object budget and ordinary-event pressure

The stock helper is:

```asm
3CE750  imul eax,[netUpdateSize],6A4h  ; 1700
3CE75A  cdq
3CE75B  and edx,7FFh
3CE761  add eax,edx
3CE763  sar eax,0Bh                   ; /2048
```

`SendNetEvents +0x283690` reads the destination cursor at
`NetPlayer[_curDst]+0x3C`, passes ring size `0x200` to its index helpers, walks
the 512-entry Classic ordinary-event ring, and finally writes the global head
`+0x9EA89C` back to that cursor. Therefore pending ordinary events are exactly
`(head - cursor) & 0x1FF`, not an inferred queue metric. Galaxy uses `0x7F`
because its corresponding ring has only 128 entries; carrying that mask into
Classic would undercount whenever the cursor/head distance crossed 128.

The replacement computes:

```text
reserve = clamp((pending - 3) * 32, 0, 200)
budget  = netUpdateSize * (1700 - reserve) / 2048
```

The two versions deliberately use different normalization domains:

| Version | `netUpdateSize` default | Stock object formula | Default object threshold |
|---|---:|---|---:|
| GameSpy/GOG | `0x400` (1024 bytes) | `size * 800 / 1024` | 800 bytes |
| Classic Patch 3 | `0x800` (2048 bytes) | `size * 1700 / 2048` | 1700 bytes |

The Classic value was confirmed directly in the Patch 3 image at
`Battlefront2.dll+0x6481A0`. The divisor `2048` is the fixed normalization
denominator used by the Classic helper; it is not a measured packet length and
does not mean every update occupies 2048 bytes. With Classic's default setting,
one scale unit happens to equal one byte, so the dynamic patch reserves 32 bytes
per pending event after the first three, capped at 200 bytes. It preserves
Classic's stock 1700-byte object threshold when no event pressure exists; it
does not change `netUpdateSize` itself.

This logical update budget is also distinct from the lower transport datagram
limit. Classic RedNet caps one datagram payload at 1024 bytes and fragments a
larger logical update. Consequently, both statements are true: Classic's game
replication layer budgets a logical update with `netUpdateSize=2048`, while its
RedNet transport emits pieces no larger than 1024 bytes. This object-budget port
does not remove fragmentation; even its minimum 1500-byte object threshold can
still require multiple transport fragments after the later update sections are
appended.

The current Galaxy patch also gives `EntityMine` both recurring-state and
creation-event priority. Classic's equivalent paths were validated independently:

- `WriteObjects +0x28C240` stores its final 64-entry object-pointer list at
  `[rsp+0x2E0]`. Its sole call to the object-budget helper is at `+0x28D391`
  (return `+0x28D396`), after the native starvation promotions. The replacement
  uses that verified return address to stable-partition exact EntityMine entity
  vtable `+0x50FFF0` to the front without disturbing relative order inside either
  partition. Vtable slot `+0x1F0` points to EntityMine's writer `+0x2B1630`.
- Stock `SendNetEvents +0x283690` walks backward from `head-1`, stops as soon as
  its 64-entry candidate heap is full, sets the destination cursor to the head,
  and then applies the ordinary-event byte limit. Under a burst, an older mine
  creation can therefore be consumed without ever entering the candidate heap.
  The replacement examines the complete pending 512-entry ring from cursor to
  head, retains the native score eligibility, fills the 64 entries with eligible
  EntityMine creation events first, then fills the remainder with ordinary
  events, and preserves the native cursor consumption, packet limit, unbounded
  override, event writer, and zero-bit terminator.
- A mine creation is not inferred from a weapon name. It must be event type zero,
  its class byte at `event+0x50` must resolve through `+0x26E870`, and that class's
  native vtable slot 3 must accept the runtime EntityMineClass RTTI id at
  `+0x691A10`. Mine heap priority is `62501-score`; ordinary priority remains
  `-score`. Selected mines therefore sort ahead of ordinary events and retain
  nearest-first order. If more than 64 eligible mines are pending, queue order
  determines which 64 enter the fixed-capacity heap.

### Locked jump

The Classic helper `+0x172210` is the expanded-layout equivalent of GOG
`+0x4EDC60`:

```text
projection  = dot(Soldier+738, Soldier+1A0)
threshold   = SoldierClass+9BC + [50448C]
normalCost  = SoldierClass+AF4
sprintCost  = SoldierClass+AF8
Energy      = Soldier+DF8 (flags at +E04)
Controllable= Soldier+380 (jump virtual at vtable +150)
```

It is called only once, at `+0x182E2B`; the second control-state branch jumps to
that common call. If a zero-cost normal jump is rejected because the projection
is only up to `0.1` above the sprint boundary, the hook invokes the same jump-state
virtual without charging Energy. All other inputs call stock code.

### Infinite sprint

The sprint handler receives the Controllable subobject and keeps the Soldier base
at `Controllable-0x380`. At `+0x18323E` it calls `RollUsingEnergy +0x17A8E0`
with that Soldier pointer, then unconditionally jumps to the update tail without
checking `AL`. The GOG handler has the same sequence at `0x4EB146`.

`RollUsingEnergy` charges `SoldierClass+0xAFC` through Energy at
`Soldier+0xDF8`, invokes the Controllable roll virtual at `+0x158`, and refunds
the charge if the physical roll fails. It returns false when Energy was already
exhausted or the roll virtual failed. The exhausted flag is bit zero at
`Soldier+0xE04`; a refund clears it only after Energy reaches its recovery
threshold. Ignoring false therefore lets an exhausted input, including an
airborne failed roll that remains below that threshold, bypass the handler's
native EndSprint virtual at `+0x1831FB` (`Controllable` vtable `+0x168`).

The patch redirects only the five-byte call at `+0x18323E`. Its wrapper returns
the stock result unchanged and invokes the same EndSprint virtual only when the
result is false and the exhausted flag remains set. Successful rolls,
non-exhausted airborne failures, and every other `RollUsingEnergy` caller retain
stock behavior.

### Pre-play disconnect

Classic shell `DropPlayer +0x2924B0` has three calls in host-shell update
`+0x297200`. It clears shell identity/key state but does not clear the reverse
guest relationship. `SetNotPlaying +0x285E30` clears the selected playing bit,
disconnects it, and clears each of the primary's three guest bits. The hook calls
`SetNotPlaying` before the original `DropPlayer`, allowing the normal old/new mask
reconciliation to reach per-slot `RemovePlayer` while retaining Classic's shell
cleanup.

### Spawn delay and pregame transition

`ScriptCB_SetSpawnDelay +0x22F000` reads two Lua numbers, then selects hardcoded
`15.0f` at `+0x22F054` whenever the effective network-enabled flag is set. It
calls `SpawnManager::SetSpawnDelay +0x324F30` for teams 1 and 2. The replacement
executes the same calls but selects the validated `SPAWN_TIMER` value. Replacing
the complete callback also preserves the Lua-provided delay for non-network games
and avoids retaining an independently allocated ProcessWriter code cave.

`UpdatePreGame +0x288400` compares elapsed integer seconds with max pregame time:

```asm
28843D  mov eax,[maxPreGame]
288443  cmp [rsp+20],eax
288447  jle 288463
288449  call VanishAllPlayers
```

Changing `JLE` to `JL` ends pregame at equality. The Vanish wrapper then resets
the expanded SpawnManager configured delays (`+0x74/+0x94`) into live timers
(`+0xB4/+0xF4`) and stamps each playing human Character's required wave
(`Character+0x1A8`) to `teamWave(+0xD4) + 1`. It traverses the bounded network
player slots and resolves ownership through `Character::FindPlayer +0x93A00`;
team comes from the Character's native `+0x164` field. The Galaxy port uses the
same algorithm with its corresponding layout offsets.

### Map watchdog

`+0x269120` is the exact Classic counterpart to the Galaxy gate. Its stock logic
returns ready immediately when networking is disabled; otherwise it establishes
a host/client deadline using `NetComm::GetTime +0x3CFC10` and latches
`+0x9C51E3` after expiry. The replacement reproduces that logic. While map status
`+0xAEFF90` is nonzero it counts calls; at 100 it returns through the same
network-disabled/ready result. Status zero resets the count.

### Vote exploit

`ReadBootRequest +0x279E60` reaches:

```asm
279F51  movzx eax,byte [rsp+24]
279F56  mov [9EA985],al
```

The patch makes EAX zero. The second site is the registered
`ScriptCB_SetNetGameDefaults +0x23F040`; `+0x23F0B6` calls the Lua `iVote` reader
with output local `[rbp-55]`. The replacement writes zero to that local and NOPs
the fifth byte.

## Intentional non-ports

### `/norender`

The Galaxy patch bypasses failed render-object construction because `/norender`
is broken in the GOG executable. That implementation is not needed in Classic.
Classic's own headless path also develops severe pacing problems, so the Aspyr
launch path removes the `/norender` token and leaves the normal rendered/device
loop active. The DLL performs no render-flag writes.

### Password

The Galaxy workaround exists because its `PasswordStr` default path reads through
a broken local-buffer route. Classic still has the string for script defaults,
but `DedicatedLaunchParameters +0xEA9C0` has a direct `/password` branch near
`+0xEAD00`: it copies the command-line value and calls `SetPassword +0x296F60`.
SWBF2Admin launches with `/password`, so redirecting the old script local would add
risk without changing the deployed path.

### GameSpy dedicated metadata

The two Galaxy edits are inside QR2 response fields named `fgr_int_r0` and
`fgd_int_serverType`. Neither string exists in Patch 3. Classic lobby metadata is
Photon/RedNet-owned; inventing an offset translation would touch an unrelated
structure.

### Dedicated render/present and inactive-window sleep

Galaxy NOPs a dedicated render/present call and changes a `Sleep(10)` to
`Sleep(0)`. Classic server testing establishes the opposite operational
constraint: removing its rendered/device work reproduces the severe
rubber-banding and jitter associated with `/norender`. The network portions of
the Galaxy patch are ported; these two process-loop edits are explicitly not.

### GameSpy direct transport

The Galaxy DLL's direct-transport subsystem hooks the original GameSpy-era
network profile and is paired with the x86 client patch. Classic uses the
unrelated Photon/RedNet path and has no validated x64 hook profile for that
feature. Its source, policy handling, tracing presets, and MinHook dependency
remain intentionally GOG-only.

## Patch sites

These stock Patch 3 preimages were recorded while validating the port. Runtime
patching targets their RVAs directly without checking the preimages:

| RVA | Expected | Action |
|---:|---|---|
| `0xC0839` | `E8 52 16 1B 00` | call the spawn-gate relay at `+0x271EE3` |
| `0x271EE3` | twelve `CC` bytes | absolute jump to the Rcon DLL spawn gate |
| `0x64AA58` | `70 63` (`pc`; other valid lobby codes accepted) | selected `PLATFORM_LOBBY` code |
| `0x279F51` | `0F B6 44 24 24` | zero vote-request byte |
| `0x23F0B6` | `E8 15 AB 00 00` | initialize `iVote` local to zero |
| `0x269120` | `48 83 EC 38 0F B6 05 A1 D4 C0 00 85 C0` | map-gate replacement |
| `0x28DA86` | `05` | `20` |
| `0x3CE7DD` | `03` | `01` |
| `0x3CE750` | `69 05 46 9A 27 00 A4 06 00 00 99 81 E2 FF 07 00 00` | object-budget replacement |
| `0x283690` | `48 89 4C 24 08 48 81 EC 88 02 00 00` | ordinary-event sender replacement |
| `0x283E2E` | `99 2B C2 D1 F8` | NOP signed divide by two |
| `0x283FB3` | `E8 58 27 FF FF` | redirect the send-window call to the CREATE-aware wrapper |
| `0x289C20` | `48 89 54 24 10` | CREATE transaction hook |
| `0x284B7F` | `73 18` | retarget full-slot branch to native pacing tail |
| `0x284C6B` | `03 C8` | `FF C1`, schedule the next eligible host turn |
| `0x172210` | `48 89 5C 24 08` | locked-jump hook |
| `0x18323E` | `E8 9D 76 FF FF` | redirect the sprint-state roll call |
| `0x2924B0` | `89 54 24 10 48 89 4C 24 08` | pre-play drop hook |
| `0x22F000` | `40 53 48 83 EC 40 48 8B 0D 2B 0B 78 00` | spawn-delay callback replacement |
| `0x288447` | `7E` | `7C` |
| `0x288770` | `48 83 EC 58 C7 44 24 20 00 00 00 00` | Vanish wrapper |
| `0x25C7E0` | `44 88 44 24 18 89 4C 24 08 55 53 56` | RCON/chat hook |

Short hooks and redirected calls use a five-byte relative branch or call to
memory allocated within two gigabytes of `Battlefront2.dll`; that relay performs
an absolute jump to the DLL. Trampolines copy only the documented,
position-independent prologue instructions listed above and return with an
absolute jump. Whole-function replacements do not construct a trampoline.

## SWBF2Admin integration

`ServerManager.InjectRconDllIfRequired` now includes `GameserverType.Aspyr`, which
selects `DllLoader_64.exe` and `RconServer_64.dll`. The release profile consumes
the new root project output at `../rconserver-aspyr/out/RconServer_64.dll`.
For Aspyr launches, `ServerManager` normalizes `ServerSettings.Platform` and
exports it as `PLATFORM_LOBBY` before creating the server process. The legacy
ProcessWriter platform/spawn-delay special cases and the XML definitions for
spawn-queue, platform lobby, spawn delay, distance relay, and map watchdog are
removed. `ServerManager` exports the configured delay as `SPAWN_TIMER` before
process creation; the RconServer callback replacement is its sole runtime owner.

## Verification completed

- x86 and x64 Debug and Release presets build cleanly with MSVC and use the same
  dynamic CRT model;
- PE machine is `0x8664`; imports include `WS2_32`, the MSVC runtime, and the
  Universal CRT;
- all patch, hook, and EntityMine support sites were confirmed against the
  current Patch 3 DLL during the static port;
- the staged `out/RconServer_64.dll` is produced by the build;
- common harness, protocol, logging, MD5, formatting, and build behavior are
  aligned between the x86 and x64 projects;
- the staged x64 DLL is rebuilt from source without runtime hash validation.

The x86 and x64 harnesses share the established client ownership and RCON
threading structure. Their low-risk safety additions include validated
`SPAWN_TIMER`, bounded CREATE/event indices, queued game-thread chat, serialized
command state, complete TCP I/O, bounded response framing, connection limits,
and finite socket-send blocking. Runtime patching in both projects targets the
supported game binary directly; x64 retains only the relay, trampoline, and
protected-write machinery required by its architecture.

Dynamic validation still requires launching a dedicated server and exercising
login, command output, chat fanout, `/lua`, map transition, and a populated match.
Static validation can prove ABI/site correctness and build integrity, but it
cannot prove that the process-level hooks coexist with every other injected mod.
