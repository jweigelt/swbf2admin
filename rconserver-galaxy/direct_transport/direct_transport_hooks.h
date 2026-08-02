#pragma once

#include "direct_transport/direct_transport_profile.h"

#include <array>
#include <cstdint>
#include <string>

namespace bf2direct
{

class ServerTransport;

class ServerHooks
{
public:
	bool Prepare(HMODULE module, const ServerProfile &profile, ServerTransport &transport, std::string &error) noexcept;
	bool Install(std::string &error) noexcept;
	bool Rollback(std::string &error) noexcept;
	std::uint32_t InstalledCount() const noexcept;
	HookId FailedHook() const noexcept;
	std::uint32_t FailureError() const noexcept;
	std::uint32_t ExpectedCount() const noexcept;

private:
	bool InstallListener(void **slot, void *expected, void *replacement, bool &installed, std::string &error) noexcept;
	bool RestoreListener(void **slot, void *expectedReplacement, void *original, bool &installed,
						 std::string &error) noexcept;

	HMODULE module_{};
	const ServerProfile *profile_{};
	ServerTransport *transport_{};
	std::array<void *, 6> targets_{};
	std::array<bool, 6> created_{};
	bool minHookInitialized_{};
	bool detoursEnabled_{};
	bool remoteListenerInstalled_{};
	bool localListenerInstalled_{};
	HookId failedHook_{};
	std::uint32_t failureError_{};
};

ServerHooks &GetServerHooks() noexcept;
void SubmitDirectNative(void *packet, void *endpoint) noexcept;

#if defined(BF2_DIRECT_ABI_TEST)
struct AbiOriginals
{
	void *finalSend{};
	void *groupSend{};
	void *receiveOrchestration{};
	void *nativeIntake{};
	void *disconnect{};
	void *reset{};
	void *remoteMember{};
	void *localLobbyLeft{};
};

void ConfigureAbiTest(const AbiOriginals &originals) noexcept;
void *HookForAbiTest(HookId id) noexcept;
#endif

} // namespace bf2direct
