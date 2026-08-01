# rconserver-aspyr

`RconServer_64.dll` is the Classic Collection/Patch 3 x64 port of
`rconserver-galaxy`. It preserves the SWBF2Admin TCP RCON protocol and ports each
Galaxy gameplay patch for which Classic contains an equivalent code path.

The runtime targets the supported Patch 3 `Battlefront2.dll` directly without
hash or preimage validation. `PatchEngine` retains memory-protection,
allocation, relay, and trampoline failure handling.

## Cross-version maintenance

The source layout intentionally mirrors `rconserver-galaxy`:

- `bf2server.h` contains all version-specific offsets, shared constants, and one
  `bf2server_patch_*` declaration per gameplay patch;
- `bf2server.cpp` contains those individually named patch implementations and a
  Galaxy-shaped `bf2server_init` patch list;
- `RconClient.h/.cpp` owns the unchanged SWBF2Admin wire protocol;
- `RconServer.h/.cpp` owns listening, client fanout, and the chat callback;
- `md5.h/.cpp` provides the same RCON password digest as Galaxy;
- `dllmain.cpp` and `Logger.*` retain the same roles.

`PatchEngine.*` is the intentional x64-only addition. It provides protected byte
writes, near relays, and trampolines that Galaxy's x86 inline assembly did not
require.

Galaxy's GameSpy direct-transport subsystem is intentionally absent. Classic
uses Photon/RedNet and has no validated equivalent hook profile.

The checked-in `.clang-format` preserves the Galaxy-style Allman braces, tab
indentation, right-aligned pointer/reference declarators, and macro alignment.
Run `clang-format -i --style=file *.cpp *.h` after adding or porting a patch.

## Build

From an x64 Visual Studio developer prompt:

```powershell
cmake --preset x64-Release
cmake --build --preset x64-Release
```

The build stages the deployable file at `out/RconServer_64.dll`, which is the
location consumed by SWBF2Admin's release profile.

## Server installation

Place these files next to the configured Classic Collection server executable:

- `RconServer_64.dll`
- `DllLoader_64.exe`

SWBF2Admin now selects the x64 loader and DLL for `GameserverType.Aspyr`.
It starts Classic's `Battlefront.exe` bootstrap and injects this DLL into that
process; the DLL waits for the bootstrap to load `Battlefront2.dll` before it
installs any game patch.
The DLL listens on TCP using the numeric `/gameport` value; the game continues to
use UDP on the same number.

The Aspyr launch path removes `/norender` from the configured argument string.
The Galaxy workaround for that flag is not installed in this DLL.

`SPAWN_TIMER` controls the network spawn delay and defaults to `15.0`, matching
Galaxy. SWBF2Admin exports it before process creation, so changing the setting
requires a server restart. The wire protocol, MD5 login challenge, `/lua ` prefix,
chat rows, and `Game has ended` notification remain compatible with SWBF2Admin.

The object-budget patch also carries Galaxy's EntityMine behavior: mine state is
serialized before ordinary recurring state, and eligible mine creation events
are selected before other ordinary events during a burst. Classic's native
scoring, 64-event selection cap, packet byte limit, and 512-entry ring semantics
remain in force.

The update scheduler visits every destination during each host send pass and
makes sent destinations eligible again on the next server turn. Classic's
native acknowledgement window and `IsPipeFull` capacity check remain active,
so eligibility does not bypass per-client or shared-pipe backpressure.

The infinite-sprint patch wraps only the sprint handler's roll call. A failed
roll ends sprint only when the stock Energy state is exhausted; successful rolls
and non-exhausted airborne failures retain their native behavior.

`RconServer_64` also owns two Classic-only startup patches. The spawn-queue fix
skips disconnected-player ticket holes without changing the HUD's raw queue
position. `PLATFORM_LOBBY` selects `pc`, `ps`, `xb`, or `ns`; SWBF2Admin passes
its Platform setting through this environment variable. Lobby selection is
consumed during Photon startup and therefore requires a server restart.

See `PORTING.md` for the patch-by-patch reverse-engineering evidence and the
intentional non-ports.
