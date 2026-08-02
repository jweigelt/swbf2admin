#pragma once

#include <atomic>
#include <cstddef>
#include <cstdint>
#include <string>

namespace bf2direct
{

class ServerTransport;

struct GalaxySystemAddress
{
	std::uint16_t family{};
	std::uint16_t portNetworkOrder{};
	std::uint32_t addressNetworkOrder{};
	std::byte reserved[12]{};
};

static_assert(sizeof(GalaxySystemAddress) == 20);
static_assert(std::atomic_uint32_t::is_always_lock_free);

class GalaxyPeerEndpoint
{
public:
	void Prepare(ServerTransport &transport, std::uint16_t gamePort) noexcept;
	void TryInstall() noexcept;
	bool Rollback(std::string &error) noexcept;
	void FinalizeAfterMinHookShutdown() noexcept;
	std::uint32_t PublicIpv4NetworkOrder() const noexcept;

	void Observe(void *rawRakPeer, const GalaxySystemAddress *result) noexcept;

#if defined(BF2_DIRECT_ABI_TEST)
	void ConfigureAbiTest(ServerTransport &transport, void *original, std::uintptr_t expectedVtable,
						  std::uint16_t gamePort) noexcept;
	void *HookForAbiTest() noexcept;
#endif

private:
	friend struct GalaxyPeerEndpointTestAccess;

	ServerTransport *transport_{};
	void *target_{};
	std::uintptr_t expectedVtable_{};
	std::uint16_t gamePort_{};
	std::atomic_uint32_t publicIpv4_{};
	bool attempted_{};
	bool created_{};
	bool enabled_{};
};

GalaxyPeerEndpoint &GetGalaxyPeerEndpoint() noexcept;

} // namespace bf2direct
