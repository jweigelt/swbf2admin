#include "direct_transport/direct_transport_hooks.h"
#include "direct_transport/direct_transport_server.h"
#include "direct_transport/galaxy_peer_endpoint.h"

#include <WinSock2.h>
#include <Windows.h>

#include <array>
#include <cstdint>
#include <type_traits>

using ResetTransport = bf2direct::ServerTransport;

static_assert(std::is_same_v<decltype(&ResetTransport::OnReset), void (ResetTransport::*)(std::uint8_t) noexcept>);

namespace
{

struct Observation
{
	std::uint32_t calls{};
	std::uint32_t destination{};
	std::uint32_t secondArgument{};
	std::uint32_t stackArgument{};
	std::uint32_t entryError{};
	std::uint32_t entryWsaError{};
};

struct CallState
{
	std::uint32_t beforeEsp{};
	std::uint32_t afterEsp{};
	std::uint32_t ebx{};
	std::uint32_t esi{};
	std::uint32_t edi{};
	std::uint32_t result{};
};

Observation gFinal{};
Observation gGroup{};
Observation gReceive{};
Observation gIntake{};
Observation gDisconnect{};
Observation gReset{};
Observation gRemote{};
Observation gLocal{};
std::uint32_t gExternalCalls{};
bf2direct::GalaxySystemAddress gExternalAddress{};

int __cdecl ObserveFinal(std::uint32_t destination, std::uint32_t bytes, std::uint32_t length) noexcept
{
	++gFinal.calls;
	gFinal.destination = destination;
	gFinal.secondArgument = bytes;
	gFinal.stackArgument = length;
	gFinal.entryError = GetLastError();
	gFinal.entryWsaError = WSAGetLastError();
	SetLastError(0x1234);
	WSASetLastError(0x2345);
	return 77;
}

void __cdecl ObserveGroup(std::uint32_t destination, std::uint32_t nativeArgument, std::uint32_t group) noexcept
{
	++gGroup.calls;
	gGroup.destination = destination;
	gGroup.secondArgument = nativeArgument;
	gGroup.stackArgument = group;
	gGroup.entryError = GetLastError();
	gGroup.entryWsaError = WSAGetLastError();
	SetLastError(0x5678);
	WSASetLastError(0x6789);
}

void __cdecl SyntheticReceive() noexcept
{
	++gReceive.calls;
	gReceive.entryError = GetLastError();
	gReceive.entryWsaError = WSAGetLastError();
	SetLastError(0x6101);
	WSASetLastError(0x7101);
}

void __fastcall SyntheticIntake(void *packet, void *endpoint) noexcept
{
	++gIntake.calls;
	gIntake.destination = reinterpret_cast<std::uint32_t>(packet);
	gIntake.secondArgument = reinterpret_cast<std::uint32_t>(endpoint);
	gIntake.entryError = GetLastError();
	gIntake.entryWsaError = WSAGetLastError();
	SetLastError(0x6102);
	WSASetLastError(0x7102);
}

void __fastcall SyntheticDisconnect(int physicalPrimary) noexcept
{
	++gDisconnect.calls;
	gDisconnect.destination = static_cast<std::uint32_t>(physicalPrimary);
	gDisconnect.entryError = GetLastError();
	gDisconnect.entryWsaError = WSAGetLastError();
	SetLastError(0x6103);
	WSASetLastError(0x7103);
}

void __fastcall SyntheticReset(int mode) noexcept
{
	++gReset.calls;
	gReset.destination = static_cast<std::uint32_t>(mode);
	gReset.entryError = GetLastError();
	gReset.entryWsaError = WSAGetLastError();
	SetLastError(0x6104);
	WSASetLastError(0x7104);
}

void __cdecl ObserveRemote(std::uint32_t self, std::uint32_t lobby, std::uint32_t member, std::uint32_t state) noexcept
{
	++gRemote.calls;
	gRemote.destination = self;
	gRemote.secondArgument = lobby;
	gRemote.stackArgument = member;
	gRemote.entryError = GetLastError();
	gRemote.entryWsaError = WSAGetLastError();
	if (state != 2) gRemote.calls = 0xffffffffu;
	SetLastError(0x6105);
	WSASetLastError(0x7105);
}

void __cdecl ObserveLocal(std::uint32_t self, std::uint32_t lobby, std::uint32_t reason) noexcept
{
	++gLocal.calls;
	gLocal.destination = self;
	gLocal.secondArgument = lobby;
	gLocal.stackArgument = reason;
	gLocal.entryError = GetLastError();
	gLocal.entryWsaError = WSAGetLastError();
	SetLastError(0x6106);
	WSASetLastError(0x7106);
}

bf2direct::GalaxySystemAddress *__fastcall SyntheticGetExternalId(void *, void *,
																  bf2direct::GalaxySystemAddress *output,
																  bf2direct::GalaxySystemAddress) noexcept
{
	++gExternalCalls;
	*output = gExternalAddress;
	SetLastError(0x6107);
	WSASetLastError(0x7107);
	return output;
}

__declspec(naked) int SyntheticFinalSend()
{
	__asm {
		push dword ptr [esp + 4]
		push edx
		push ecx
		call ObserveFinal
		add  esp, 12
		ret
	}
}

__declspec(naked) void SyntheticGroupSend()
{
	__asm {
		push dword ptr [esp + 4]
		push edx
		push ecx
		call ObserveGroup
		add  esp, 12
		ret
	}
}

__declspec(naked) void SyntheticRemoteMember()
{
	__asm {
		push dword ptr [esp + 12]
		push dword ptr [esp + 12]
		push dword ptr [esp + 12]
		push ecx
		call ObserveRemote
		add  esp, 16
		ret  12
	}
}

__declspec(naked) void SyntheticLocalLobbyLeft()
{
	__asm {
		push dword ptr [esp + 8]
		push dword ptr [esp + 8]
		push ecx
		call ObserveLocal
		add  esp, 12
		ret  8
	}
}

__declspec(naked) void InvokeFinalSend(void *, std::uint32_t, std::uint32_t, std::uint32_t, CallState *)
{
	__asm {
		push ebp
		mov  ebp, esp
		push ebx
		push esi
		push edi
		mov  ebx, 0x11223344
		mov  esi, 0x55667788
		mov  edi, 0x99aabbcc
		mov  ecx, dword ptr [ebp + 12]
		mov  edx, dword ptr [ebp + 16]
		push dword ptr [ebp + 20]
		mov  eax, dword ptr [ebp + 24]
		mov  dword ptr [eax], esp
		call dword ptr [ebp + 8]
		mov  ecx, dword ptr [ebp + 24]
		mov  dword ptr [ecx + 4], esp
		mov  dword ptr [ecx + 8], ebx
		mov  dword ptr [ecx + 12], esi
		mov  dword ptr [ecx + 16], edi
		mov  dword ptr [ecx + 20], eax
		add  esp, 4
		pop  edi
		pop  esi
		pop  ebx
		mov  esp, ebp
		pop  ebp
		ret
	}
}

__declspec(naked) void InvokeGroupSend(void *, std::uint32_t, std::uint32_t, std::uint32_t, CallState *)
{
	__asm {
		push ebp
		mov  ebp, esp
		push ebx
		push esi
		push edi
		mov  ebx, 0x11223344
		mov  esi, 0x55667788
		mov  edi, 0x99aabbcc
		mov  ecx, dword ptr [ebp + 12]
		mov  edx, dword ptr [ebp + 16]
		push dword ptr [ebp + 20]
		mov  eax, dword ptr [ebp + 24]
		mov  dword ptr [eax], esp
		call dword ptr [ebp + 8]
		mov  ecx, dword ptr [ebp + 24]
		mov  dword ptr [ecx + 4], esp
		mov  dword ptr [ecx + 8], ebx
		mov  dword ptr [ecx + 12], esi
		mov  dword ptr [ecx + 16], edi
		add  esp, 4
		pop  edi
		pop  esi
		pop  ebx
		mov  esp, ebp
		pop  ebp
		ret
	}
}

bool RegistersAndStackMatch(const CallState &state) noexcept
{
	return state.beforeEsp == state.afterEsp && state.ebx == 0x11223344 && state.esi == 0x55667788 &&
		   state.edi == 0x99aabbcc;
}

} // namespace

int main()
{
	SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX);
	bf2direct::AbiOriginals originals{};
	originals.finalSend = reinterpret_cast<void *>(&SyntheticFinalSend);
	originals.groupSend = reinterpret_cast<void *>(&SyntheticGroupSend);
	originals.receiveOrchestration = reinterpret_cast<void *>(&SyntheticReceive);
	originals.nativeIntake = reinterpret_cast<void *>(&SyntheticIntake);
	originals.disconnect = reinterpret_cast<void *>(&SyntheticDisconnect);
	originals.reset = reinterpret_cast<void *>(&SyntheticReset);
	originals.remoteMember = reinterpret_cast<void *>(&SyntheticRemoteMember);
	originals.localLobbyLeft = reinterpret_cast<void *>(&SyntheticLocalLobbyLeft);
	bf2direct::ConfigureAbiTest(originals);

	std::array<std::uint8_t, 1009> finalBytes{};
	finalBytes[0] = 1;
	CallState finalState{};
	SetLastError(0xAAAA);
	WSASetLastError(0xAABB);
	InvokeFinalSend(bf2direct::HookForAbiTest(bf2direct::HookId::FinalSend), 17,
					reinterpret_cast<std::uint32_t>(finalBytes.data()), 1009, &finalState);
	if (!RegistersAndStackMatch(finalState) || finalState.result != 77 || gFinal.calls != 1 ||
		gFinal.destination != 17 || gFinal.secondArgument != reinterpret_cast<std::uint32_t>(finalBytes.data()) ||
		gFinal.stackArgument != 1009 || gFinal.entryError != 0xAABB || gFinal.entryWsaError != 0xAABB ||
		GetLastError() != 0x2345 || WSAGetLastError() != 0x2345)
	{
		return 1;
	}

	CallState groupState{};
	SetLastError(0xBBBB);
	WSASetLastError(0xBBCC);
	InvokeGroupSend(bf2direct::HookForAbiTest(bf2direct::HookId::GroupSend), 23, 0x50607080, 0x90A0B0C0, &groupState);
	if (!RegistersAndStackMatch(groupState) || gGroup.calls != 1 || gGroup.destination != 23 ||
		gGroup.secondArgument != 0x50607080 || gGroup.stackArgument != 0x90A0B0C0 || gGroup.entryError != 0xBBCC ||
		gGroup.entryWsaError != 0xBBCC || GetLastError() != 0x6789 || WSAGetLastError() != 0x6789)
	{
		return 2;
	}

	using ReceiveHook = void(__cdecl *)();
	using IntakeHook = void(__fastcall *)(void *, void *);
	using IntegerHook = void(__fastcall *)(int);
	using RemoteHook = void(__fastcall *)(void *, void *, const void *, const void *, std::uint32_t);
	using LocalHook = void(__fastcall *)(void *, void *, const void *, std::uint32_t);

	SetLastError(0x6001);
	WSASetLastError(0x7001);
	reinterpret_cast<ReceiveHook>(bf2direct::HookForAbiTest(bf2direct::HookId::ReceiveOrchestration))();
	if (gReceive.calls != 1 || gReceive.entryError != 0x7001 || gReceive.entryWsaError != 0x7001 ||
		GetLastError() != 0x7101 || WSAGetLastError() != 0x7101)
	{
		return 3;
	}

	std::array<std::uint8_t, 32> packet{};
	packet[6] = 5;
	SetLastError(0x6002);
	WSASetLastError(0x7002);
	reinterpret_cast<IntakeHook>(bf2direct::HookForAbiTest(bf2direct::HookId::NativeIntake))(
		packet.data(), reinterpret_cast<void *>(0x12345678));
	if (gIntake.calls != 1 || gIntake.destination != reinterpret_cast<std::uint32_t>(packet.data()) ||
		gIntake.secondArgument != 0x12345678 || gIntake.entryError != 0x7002 || gIntake.entryWsaError != 0x7002 ||
		GetLastError() != 0x7102 || WSAGetLastError() != 0x7102)
	{
		return 4;
	}

	SetLastError(0x6003);
	WSASetLastError(0x7003);
	reinterpret_cast<IntegerHook>(bf2direct::HookForAbiTest(bf2direct::HookId::Disconnect))(31);
	if (gDisconnect.calls != 1 || gDisconnect.destination != 31 || gDisconnect.entryError != 0x7003 ||
		gDisconnect.entryWsaError != 0x7003 || GetLastError() != 0x7103 || WSAGetLastError() != 0x7103)
	{
		return 5;
	}

	SetLastError(0x6004);
	WSASetLastError(0x7004);
	reinterpret_cast<IntegerHook>(bf2direct::HookForAbiTest(bf2direct::HookId::Reset))(1);
	if (gReset.calls != 1 || gReset.destination != 1 || gReset.entryError != 0x7004 || gReset.entryWsaError != 0x7004 ||
		GetLastError() != 0x7104 || WSAGetLastError() != 0x7104)
	{
		return 6;
	}

	SetLastError(0x6005);
	WSASetLastError(0x7005);
	reinterpret_cast<RemoteHook>(bf2direct::HookForAbiTest(bf2direct::HookId::RemoteMemberListener))(
		reinterpret_cast<void *>(0x11111111), nullptr, reinterpret_cast<void *>(0x22222222),
		reinterpret_cast<void *>(0x33333333), 2);
	if (gRemote.calls != 1 || gRemote.destination != 0x11111111 || gRemote.secondArgument != 0x22222222 ||
		gRemote.stackArgument != 0x33333333 || gRemote.entryError != 0x7005 || gRemote.entryWsaError != 0x7005 ||
		GetLastError() != 0x7105 || WSAGetLastError() != 0x7105)
	{
		return 7;
	}

	SetLastError(0x6006);
	WSASetLastError(0x7006);
	reinterpret_cast<LocalHook>(bf2direct::HookForAbiTest(bf2direct::HookId::LocalLobbyLeftListener))(
		reinterpret_cast<void *>(0x44444444), nullptr, reinterpret_cast<void *>(0x55555555), 0x66666666);
	if (gLocal.calls != 1 || gLocal.destination != 0x44444444 || gLocal.secondArgument != 0x55555555 ||
		gLocal.stackArgument != 0x66666666 || gLocal.entryError != 0x7006 || gLocal.entryWsaError != 0x7006 ||
		GetLastError() != 0x7106 || WSAGetLastError() != 0x7106)
	{
		return 8;
	}
	struct SyntheticRakPeer
	{
		std::uintptr_t vtable;
	};
	constexpr std::uintptr_t expectedVtable = 0x10203040;
	SyntheticRakPeer peer{expectedVtable};
	bf2direct::ServerTransport endpointTransport;
	endpointTransport.Arm();
	auto &endpoint = bf2direct::GetGalaxyPeerEndpoint();
	endpoint.ConfigureAbiTest(endpointTransport, reinterpret_cast<void *>(&SyntheticGetExternalId), expectedVtable,
							  3658);
	bf2direct::GalaxySystemAddress observer{};
	observer.family = AF_INET;
	observer.portNetworkOrder = htons(45000);
	observer.addressNetworkOrder = htonl(0x01010101);
	gExternalAddress.family = AF_INET;
	gExternalAddress.portNetworkOrder = htons(46000);
	gExternalAddress.addressNetworkOrder = htonl(0x08080808);
	bf2direct::GalaxySystemAddress output{};
	using ExternalHook = bf2direct::GalaxySystemAddress *(
		__fastcall *)(void *, void *, bf2direct::GalaxySystemAddress *, bf2direct::GalaxySystemAddress);
	peer.vtable = 0x50607080;
	reinterpret_cast<ExternalHook>(endpoint.HookForAbiTest())(&peer, nullptr, &output, observer);
	if (endpoint.PublicIpv4NetworkOrder() != 0)
	{
		return 9;
	}
	peer.vtable = expectedVtable;
	gExternalAddress.addressNetworkOrder = htonl(0xC0A80101);
	reinterpret_cast<ExternalHook>(endpoint.HookForAbiTest())(&peer, nullptr, &output, observer);
	gExternalAddress.addressNetworkOrder = htonl(0xC0000201);
	reinterpret_cast<ExternalHook>(endpoint.HookForAbiTest())(&peer, nullptr, &output, observer);
	if (endpoint.PublicIpv4NetworkOrder() != 0)
	{
		return 10;
	}
	gExternalAddress.addressNetworkOrder = htonl(0x08080808);
	SetLastError(0x6007);
	WSASetLastError(0x7007);
	const auto result = reinterpret_cast<ExternalHook>(endpoint.HookForAbiTest())(&peer, nullptr, &output, observer);
	if (result != &output || gExternalCalls != 4 || output.addressNetworkOrder != htonl(0x08080808) ||
		endpoint.PublicIpv4NetworkOrder() != htonl(0x08080808) || GetLastError() != 0x7107 ||
		WSAGetLastError() != 0x7107)
	{
		return 11;
	}
	gExternalAddress.addressNetworkOrder = htonl(0x09090909);
	reinterpret_cast<ExternalHook>(endpoint.HookForAbiTest())(&peer, nullptr, &output, observer);
	if (gExternalCalls != 5 || endpoint.PublicIpv4NetworkOrder() != htonl(0x08080808))
	{
		return 12;
	}
	return 0;
}
