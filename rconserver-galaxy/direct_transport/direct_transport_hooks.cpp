#include "direct_transport/direct_transport_hooks.h"

#include "direct_transport/direct_transport_server.h"

#include <MinHook.h>
#include <WinSock2.h>
#include <Windows.h>

#include <algorithm>
#include <array>
#include <cstddef>
#include <cstdint>

namespace bf2direct
{
namespace
{

using ReceiveOrchestrationFn = void(__cdecl *)();
using NativeIntakeFn = void(__fastcall *)(void *, void *);
using DisconnectFn = void(__fastcall *)(int);
using ResetFn = void(__fastcall *)(int);
using RemoteMemberFn = void(__thiscall *)(void *, const void *, const void *, std::uint32_t);
using LocalLobbyLeftFn = void(__thiscall *)(void *, const void *, std::uint32_t);

ServerTransport *gServerTransport{};
void *gFinalSendOriginal{};
void *gGroupSendOriginal{};
ReceiveOrchestrationFn gReceiveOrchestrationOriginal{};
NativeIntakeFn gNativeIntakeOriginal{};
DisconnectFn gDisconnectOriginal{};
ResetFn gResetOriginal{};
RemoteMemberFn gRemoteMemberOriginal{};
LocalLobbyLeftFn gLocalLobbyLeftOriginal{};
thread_local int gTransmitGroupPrimary = -1;

void AppendError(std::string &error, const char *text, std::uint32_t code)
{
	if (!error.empty()) error += "; ";
	error += text;
	if (code) error += ": " + std::to_string(code);
}

// Both game functions leave their one stack argument for the caller.
__declspec(naked) int __fastcall CallFinalSendOriginal(int, const void *, int)
{
	__asm {
		push dword ptr [esp + 4]
		call dword ptr [gFinalSendOriginal]
		add  esp, 4
		ret  4
	}
}

int __fastcall OnFinalSend(int destination, const void *bytes, int length, const void *) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	NativeTransmitResult transmit{};
	if (gServerTransport && gServerTransport->Armed() && bytes && length > 0)
	{
		transmit = gServerTransport->TransmitNative(
			destination, gTransmitGroupPrimary,
			{static_cast<const std::uint8_t *>(bytes), static_cast<std::size_t>(length)});
	}
	const auto result = transmit.handled ? transmit.result : CallFinalSendOriginal(destination, bytes, length);
	if (transmit.handled && transmit.result < 0) WSASetLastError(transmit.error);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	WSASetLastError(wsaError);
	SetLastError(winError);
	return result;
}

__declspec(naked) int HookFinalSend()
{
	__asm {
		push dword ptr [esp]
		push dword ptr [esp + 8]
		call OnFinalSend
		ret
	}
}

__declspec(naked) void __fastcall CallGroupSendOriginal(int, void *, void *)
{
	__asm {
		push dword ptr [esp + 4]
		call dword ptr [gGroupSendOriginal]
		add  esp, 4
		ret  4
	}
}

void __fastcall OnGroupSend(int destination, void *nativeArgument, void *group, const void *) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	const auto previousGroupPrimary = gTransmitGroupPrimary;
	auto groupPrimary = -1;
	if (gServerTransport && gServerTransport->Armed())
	{
		gServerTransport->OnNativeTransmit(destination);
		groupPrimary = gServerTransport->BeginTransmitGroup(destination);
		gTransmitGroupPrimary = groupPrimary;
	}
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	CallGroupSendOriginal(destination, nativeArgument, group);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	if (gServerTransport && groupPrimary >= 0) gServerTransport->EndTransmitGroup(groupPrimary);
	gTransmitGroupPrimary = previousGroupPrimary;
	WSASetLastError(wsaError);
	SetLastError(winError);
}

__declspec(naked) void HookGroupSend()
{
	__asm {
		push dword ptr [esp]
		push dword ptr [esp + 8]
		call OnGroupSend
		ret
	}
}

void __cdecl HookReceiveOrchestration()
{
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed())
	{
		gServerTransport->BeforeReceive();
	}
	WSASetLastError(wsaError);
	SetLastError(winError);
	gReceiveOrchestrationOriginal();
	const auto winErrorAfter = GetLastError();
	const auto wsaErrorAfter = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed()) gServerTransport->AfterReceive();
	WSASetLastError(wsaErrorAfter);
	SetLastError(winErrorAfter);
}

void __fastcall HookNativeIntake(void *packet, void *endpoint) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	gNativeIntakeOriginal(packet, endpoint);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed()) gServerTransport->OnNativeIntake(endpoint);
	WSASetLastError(wsaError);
	SetLastError(winError);
}

void __fastcall HookDisconnect(int physicalPrimary) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed()) gServerTransport->OnNativeDisconnect(physicalPrimary);
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	gDisconnectOriginal(physicalPrimary);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed())
	{
		gServerTransport->OnNativeDisconnectComplete(physicalPrimary);
	}
	WSASetLastError(wsaError);
	SetLastError(winError);
}

void __fastcall HookReset(int mode) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	const auto resetMode = static_cast<std::uint8_t>(mode);
	if (gServerTransport && gServerTransport->Armed()) gServerTransport->OnReset(resetMode);
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	gResetOriginal(mode);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	WSASetLastError(wsaError);
	SetLastError(winError);
}

void __fastcall HookRemoteMember(void *self, void *, const void *lobbyId, const void *memberId,
								 std::uint32_t state) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed()) gServerTransport->OnRemoteMember(memberId, state);
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	gRemoteMemberOriginal(self, lobbyId, memberId, state);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	WSASetLastError(wsaError);
	SetLastError(winError);
}

void __fastcall HookLocalLobbyLeft(void *self, void *, const void *lobbyId, std::uint32_t reason) noexcept
{
	const auto winErrorBefore = GetLastError();
	const auto wsaErrorBefore = WSAGetLastError();
	if (gServerTransport && gServerTransport->Armed()) gServerTransport->OnLocalLobbyLeft();
	WSASetLastError(wsaErrorBefore);
	SetLastError(winErrorBefore);
	gLocalLobbyLeftOriginal(self, lobbyId, reason);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	WSASetLastError(wsaError);
	SetLastError(winError);
}

struct DetourSpec
{
	HookId id;
	void *detour;
	void **original;
};

const std::array<DetourSpec, 6> kDetours{{
	{HookId::FinalSend, reinterpret_cast<void *>(&HookFinalSend), &gFinalSendOriginal},
	{HookId::GroupSend, reinterpret_cast<void *>(&HookGroupSend), &gGroupSendOriginal},
	{HookId::ReceiveOrchestration, reinterpret_cast<void *>(&HookReceiveOrchestration),
	 reinterpret_cast<void **>(&gReceiveOrchestrationOriginal)},
	{HookId::NativeIntake, reinterpret_cast<void *>(&HookNativeIntake),
	 reinterpret_cast<void **>(&gNativeIntakeOriginal)},
	{HookId::Disconnect, reinterpret_cast<void *>(&HookDisconnect), reinterpret_cast<void **>(&gDisconnectOriginal)},
	{HookId::Reset, reinterpret_cast<void *>(&HookReset), reinterpret_cast<void **>(&gResetOriginal)},
}};

} // namespace

void SubmitDirectNative(void *packet, void *endpoint) noexcept
{
	HookNativeIntake(packet, endpoint);
}

ServerHooks &GetServerHooks() noexcept
{
	static ServerHooks hooks;
	return hooks;
}

#if defined(BF2_DIRECT_ABI_TEST)
void ConfigureAbiTest(const AbiOriginals &originals) noexcept
{
	static ServerTransport transport;
	gFinalSendOriginal = originals.finalSend;
	gGroupSendOriginal = originals.groupSend;
	gReceiveOrchestrationOriginal = reinterpret_cast<ReceiveOrchestrationFn>(originals.receiveOrchestration);
	gNativeIntakeOriginal = reinterpret_cast<NativeIntakeFn>(originals.nativeIntake);
	gDisconnectOriginal = reinterpret_cast<DisconnectFn>(originals.disconnect);
	gResetOriginal = reinterpret_cast<ResetFn>(originals.reset);
	gRemoteMemberOriginal = reinterpret_cast<RemoteMemberFn>(originals.remoteMember);
	gLocalLobbyLeftOriginal = reinterpret_cast<LocalLobbyLeftFn>(originals.localLobbyLeft);
	transport.Arm();
	gServerTransport = &transport;
}

void *HookForAbiTest(HookId id) noexcept
{
	switch (id)
	{
	case HookId::FinalSend:
		return reinterpret_cast<void *>(&HookFinalSend);
	case HookId::GroupSend:
		return reinterpret_cast<void *>(&HookGroupSend);
	case HookId::ReceiveOrchestration:
		return reinterpret_cast<void *>(&HookReceiveOrchestration);
	case HookId::NativeIntake:
		return reinterpret_cast<void *>(&HookNativeIntake);
	case HookId::Disconnect:
		return reinterpret_cast<void *>(&HookDisconnect);
	case HookId::Reset:
		return reinterpret_cast<void *>(&HookReset);
	case HookId::RemoteMemberListener:
		return reinterpret_cast<void *>(&HookRemoteMember);
	case HookId::LocalLobbyLeftListener:
		return reinterpret_cast<void *>(&HookLocalLobbyLeft);
	default:
		return nullptr;
	}
}
#endif

bool ServerHooks::Prepare(HMODULE module, const ServerProfile &profile, ServerTransport &transport,
						  std::string &error) noexcept
{
	module_ = module;
	profile_ = &profile;
	transport_ = &transport;
	return VerifyServerProfile(module, profile, error);
}

bool ServerHooks::Install(std::string &error) noexcept
{
	failedHook_ = {};
	failureError_ = 0;
	if (!module_ || !profile_ || !transport_)
	{
		error = "direct transport hooks were not prepared";
		return false;
	}
	if (!VerifyServerProfile(module_, *profile_, error)) return false;

	const auto initialized = MH_Initialize();
	if (initialized != MH_OK)
	{
		failedHook_ = HookId::HookRuntime;
		failureError_ = static_cast<std::uint32_t>(initialized);
		error = "MinHook initialization failed: " + std::to_string(initialized);
		return false;
	}
	minHookInitialized_ = true;
	gServerTransport = transport_;
	auto *const base = reinterpret_cast<std::byte *>(module_);
	for (std::size_t index{}; index < kDetours.size(); ++index)
	{
		const auto *hook = FindHookSite(profile_->hooks, kDetours[index].id);
		if (!hook)
		{
			failedHook_ = HookId::HookRuntime;
			failureError_ = ERROR_INVALID_DATA;
			error = "direct transport hook profile is incomplete";
			return false;
		}
		targets_[index] = base + hook->rva;
		const auto status = MH_CreateHook(targets_[index], kDetours[index].detour, kDetours[index].original);
		if (status != MH_OK)
		{
			failedHook_ = hook->id;
			failureError_ = static_cast<std::uint32_t>(status);
			error = std::string(hook->name) + " detour creation failed: " + std::to_string(status);
			return false;
		}
		created_[index] = true;
		const auto queued = MH_QueueEnableHook(targets_[index]);
		if (queued != MH_OK)
		{
			failedHook_ = hook->id;
			failureError_ = static_cast<std::uint32_t>(queued);
			error = std::string(hook->name) + " detour enable could not be queued: " + std::to_string(queued);
			return false;
		}
	}
	const auto applyStatus = MH_ApplyQueued();
	if (applyStatus != MH_OK)
	{
		detoursEnabled_ = true;
		failedHook_ = HookId::HookRuntime;
		failureError_ = static_cast<std::uint32_t>(applyStatus);
		error = "detour enable failed: " + std::to_string(applyStatus);
		return false;
	}
	detoursEnabled_ = true;
	gRemoteMemberOriginal = reinterpret_cast<RemoteMemberFn>(base + profile_->remoteMemberCallbackRva);
	gLocalLobbyLeftOriginal = reinterpret_cast<LocalLobbyLeftFn>(base + profile_->localLobbyLeftCallbackRva);
	failedHook_ = HookId::RemoteMemberListener;
	if (!InstallListener(reinterpret_cast<void **>(base + profile_->remoteMemberListenerSlotRva),
						 reinterpret_cast<void *>(gRemoteMemberOriginal), reinterpret_cast<void *>(&HookRemoteMember),
						 remoteListenerInstalled_, error))
	{
		return false;
	}
	failedHook_ = HookId::LocalLobbyLeftListener;
	if (!InstallListener(reinterpret_cast<void **>(base + profile_->localLobbyLeftListenerSlotRva),
						 reinterpret_cast<void *>(gLocalLobbyLeftOriginal),
						 reinterpret_cast<void *>(&HookLocalLobbyLeft), localListenerInstalled_, error))
	{
		return false;
	}
	failedHook_ = {};
	failureError_ = 0;
	return true;
}

bool ServerHooks::InstallListener(void **slot, void *expected, void *replacement, bool &installed,
								  std::string &error) noexcept
{
	if (!slot || !expected || !replacement || *slot != expected)
	{
		failureError_ = ERROR_INVALID_DATA;
		error = "listener slot changed before installation";
		return false;
	}
	DWORD originalProtection{};
	if (!VirtualProtect(slot, sizeof(*slot), PAGE_READWRITE, &originalProtection))
	{
		failureError_ = GetLastError();
		error = "listener VirtualProtect(write) failed: " + std::to_string(failureError_);
		return false;
	}
	InterlockedExchangePointer(reinterpret_cast<PVOID volatile *>(slot), replacement);
	installed = true;
	DWORD ignored{};
	if (!VirtualProtect(slot, sizeof(*slot), originalProtection, &ignored))
	{
		const auto restoreError = GetLastError();
		failureError_ = restoreError;
		InterlockedExchangePointer(reinterpret_cast<PVOID volatile *>(slot), expected);
		DWORD retryIgnored{};
		if (VirtualProtect(slot, sizeof(*slot), originalProtection, &retryIgnored) && *slot == expected)
		{
			installed = false;
		}
		error = "listener VirtualProtect(restore) failed: " + std::to_string(restoreError);
		return false;
	}
	if (*slot != replacement)
	{
		failureError_ = ERROR_WRITE_FAULT;
		error = "listener pointer write was not retained";
		return false;
	}
	return true;
}

bool ServerHooks::RestoreListener(void **slot, void *expectedReplacement, void *original, bool &installed,
								  std::string &error) noexcept
{
	if (!installed) return true;
	if (*slot != expectedReplacement)
	{
		AppendError(error, "listener slot changed before rollback", 0);
		return false;
	}
	DWORD originalProtection{};
	if (!VirtualProtect(slot, sizeof(*slot), PAGE_READWRITE, &originalProtection))
	{
		AppendError(error, "listener rollback VirtualProtect(write) failed", GetLastError());
		return false;
	}
	InterlockedExchangePointer(reinterpret_cast<PVOID volatile *>(slot), original);
	DWORD ignored{};
	auto restored = VirtualProtect(slot, sizeof(*slot), originalProtection, &ignored);
	if (!restored)
	{
		const auto restoreError = GetLastError();
		DWORD retryIgnored{};
		restored = VirtualProtect(slot, sizeof(*slot), originalProtection, &retryIgnored);
		if (!restored)
		{
			AppendError(error, "listener rollback VirtualProtect(restore) failed", restoreError);
		}
	}
	if (restored && *slot == original) installed = false;
	return restored != FALSE && !installed;
}

bool ServerHooks::Rollback(std::string &error) noexcept
{
	if (transport_) transport_->Disarm();
	bool complete = true;
	if (module_ && profile_)
	{
		auto *const base = reinterpret_cast<std::byte *>(module_);
		if (!RestoreListener(reinterpret_cast<void **>(base + profile_->localLobbyLeftListenerSlotRva),
							 reinterpret_cast<void *>(&HookLocalLobbyLeft),
							 reinterpret_cast<void *>(gLocalLobbyLeftOriginal), localListenerInstalled_, error))
		{
			complete = false;
		}
		if (!RestoreListener(reinterpret_cast<void **>(base + profile_->remoteMemberListenerSlotRva),
							 reinterpret_cast<void *>(&HookRemoteMember),
							 reinterpret_cast<void *>(gRemoteMemberOriginal), remoteListenerInstalled_, error))
		{
			complete = false;
		}
	}
	if (minHookInitialized_)
	{
		bool canRemoveDetours = !detoursEnabled_;
		if (detoursEnabled_)
		{
			bool queued = true;
			for (std::size_t index{}; index < targets_.size(); ++index)
			{
				if (!created_[index]) continue;
				const auto status = MH_QueueDisableHook(targets_[index]);
				if (status != MH_OK)
				{
					AppendError(error, "detour disable could not be queued", static_cast<std::uint32_t>(status));
					queued = false;
				}
			}
			const auto disabled = queued ? MH_ApplyQueued() : MH_UNKNOWN;
			if (disabled != MH_OK)
			{
				if (queued)
				{
					AppendError(error, "detour disable failed", static_cast<std::uint32_t>(disabled));
				}
				complete = false;
			}
			else
			{
				detoursEnabled_ = false;
				canRemoveDetours = true;
			}
		}
		if (canRemoveDetours)
		{
			for (std::size_t index{}; index < targets_.size(); ++index)
			{
				if (!created_[index]) continue;
				const auto removed = MH_RemoveHook(targets_[index]);
				if (removed != MH_OK)
				{
					AppendError(error, "detour removal failed", static_cast<std::uint32_t>(removed));
					complete = false;
					continue;
				}
				created_[index] = false;
			}
			if (MH_Uninitialize() != MH_OK)
			{
				AppendError(error, "MinHook shutdown failed", 0);
				complete = false;
			}
			else
			{
				minHookInitialized_ = false;
			}
		}
	}
	return complete;
}

std::uint32_t ServerHooks::InstalledCount() const noexcept
{
	return static_cast<std::uint32_t>(std::count(created_.begin(), created_.end(), true)) +
		   (remoteListenerInstalled_ ? 1u : 0u) + (localListenerInstalled_ ? 1u : 0u);
}

HookId ServerHooks::FailedHook() const noexcept
{
	return failedHook_;
}

std::uint32_t ServerHooks::FailureError() const noexcept
{
	return failureError_;
}

std::uint32_t ServerHooks::ExpectedCount() const noexcept
{
	return static_cast<std::uint32_t>(targets_.size()) + 2;
}

} // namespace bf2direct
