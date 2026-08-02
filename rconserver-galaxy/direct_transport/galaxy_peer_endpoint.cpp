#include "direct_transport/galaxy_peer_endpoint.h"

#include "direct_transport/direct_transport_profile.h"
#include "direct_transport/direct_transport_server.h"

#include <bf2direct/image.h>

#include <MinHook.h>

#include <WinSock2.h>
#include <Windows.h>

#include <array>
#include <cstring>

namespace bf2direct
{
namespace
{

struct GalaxyPeerProfile
{
	std::uint32_t timestamp{};
	std::uint32_t imageSize{};
	std::uint32_t getExternalIdRva{};
	std::uint32_t rawVtableRva{};
	std::array<std::uint8_t, 8> entryBytes{};
};

constexpr std::array<GalaxyPeerProfile, 2> kProfiles{{
	{0x59E6304A, 0x00AC7000, 0x0048A970, 0x0087AE64, {0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x14, 0x57, 0x8B}},
	{0x5BBE22A6, 0x00AF3000, 0x00475C50, 0x008B107C, {0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x14, 0x57, 0x8B}},
}};

using GetExternalIdFn = GalaxySystemAddress *(__thiscall *)(void *, GalaxySystemAddress *, GalaxySystemAddress);

std::atomic<GalaxyPeerEndpoint *> gEndpoint{};
GetExternalIdFn gGetExternalId{};

const GalaxyPeerProfile *SelectProfile(HMODULE module) noexcept
{
	PeIdentity identity{};
	if (!ReadPeIdentity(module, identity) || identity.machine != IMAGE_FILE_MACHINE_I386) return nullptr;
	for (const auto &profile : kProfiles)
	{
		if (profile.timestamp == identity.timestamp && profile.imageSize == identity.imageSize) return &profile;
	}
	return nullptr;
}

EndpointAddressClass AddressClass(std::uint32_t networkOrder) noexcept
{
	if (!networkOrder) return EndpointAddressClass::Unknown;
	const auto address = ntohl(networkOrder);
	const auto first = address >> 24;
	if (first == 127) return EndpointAddressClass::Loopback;
	return IsPublicIpv4(networkOrder) ? EndpointAddressClass::Public : EndpointAddressClass::Private;
}

GalaxySystemAddress *__fastcall HookGetExternalId(void *rawRakPeer, void *, GalaxySystemAddress *output,
												  GalaxySystemAddress observer) noexcept
{
	const auto result = gGetExternalId(rawRakPeer, output, observer);
	const auto winError = GetLastError();
	const auto wsaError = WSAGetLastError();
	if (auto *endpoint = gEndpoint.load(std::memory_order_acquire))
	{
		endpoint->Observe(rawRakPeer, result);
	}
	WSASetLastError(wsaError);
	SetLastError(winError);
	return result;
}

void AppendError(std::string &error, const char *text, std::uint32_t code) noexcept
{
	if (!error.empty()) error += "; ";
	error += text;
	if (code) error += ": " + std::to_string(code);
}

} // namespace

GalaxyPeerEndpoint &GetGalaxyPeerEndpoint() noexcept
{
	static GalaxyPeerEndpoint endpoint;
	return endpoint;
}

void GalaxyPeerEndpoint::Prepare(ServerTransport &transport, std::uint16_t gamePort) noexcept
{
	transport_ = &transport;
	gamePort_ = gamePort;
	target_ = nullptr;
	expectedVtable_ = 0;
	publicIpv4_.store(0, std::memory_order_release);
	attempted_ = false;
	created_ = false;
	enabled_ = false;
}

void GalaxyPeerEndpoint::TryInstall() noexcept
{
	if (attempted_ || !transport_ || !transport_->Armed()) return;
	const auto module = GetModuleHandleW(L"GalaxyPeer.dll");
	if (!module) return;
	attempted_ = true;
	const auto *profile = SelectProfile(module);
	if (!profile)
	{
		transport_->ReportEndpointObserverFailure("unsupported GalaxyPeer.dll", ERROR_BAD_EXE_FORMAT);
		return;
	}

	expectedVtable_ = reinterpret_cast<std::uintptr_t>(module) + profile->rawVtableRva;
	target_ = reinterpret_cast<std::byte *>(module) + profile->getExternalIdRva;
	const auto verified = ReadableMemoryRange(target_, profile->entryBytes.size()) &&
						  std::memcmp(target_, profile->entryBytes.data(), profile->entryBytes.size()) == 0;
	if (!verified)
	{
		transport_->ReportEndpointObserverFailure("GalaxyPeer bytes differ");
		return;
	}

	gEndpoint.store(this, std::memory_order_release);
	auto status = MH_CreateHook(target_, reinterpret_cast<void *>(&HookGetExternalId),
								reinterpret_cast<void **>(&gGetExternalId));
	if (status != MH_OK)
	{
		gEndpoint.store(nullptr, std::memory_order_release);
		transport_->ReportEndpointObserverFailure("observer installation failed", static_cast<std::uint32_t>(status));
		return;
	}
	created_ = true;
	status = MH_EnableHook(target_);
	if (status != MH_OK)
	{
		MH_RemoveHook(target_);
		created_ = false;
		gEndpoint.store(nullptr, std::memory_order_release);
		transport_->ReportEndpointObserverFailure("observer enable failed", static_cast<std::uint32_t>(status));
		return;
	}
	enabled_ = true;
}

bool GalaxyPeerEndpoint::Rollback(std::string &error) noexcept
{
	if (!created_) return true;
	if (enabled_)
	{
		const auto status = MH_DisableHook(target_);
		if (status != MH_OK)
		{
			AppendError(error, "GalaxyPeer endpoint hook disable failed", static_cast<std::uint32_t>(status));
			return false;
		}
		enabled_ = false;
	}
	const auto status = MH_RemoveHook(target_);
	if (status != MH_OK)
	{
		AppendError(error, "GalaxyPeer endpoint hook removal failed", static_cast<std::uint32_t>(status));
		return false;
	}
	created_ = false;
	gEndpoint.store(nullptr, std::memory_order_release);
	return true;
}

void GalaxyPeerEndpoint::FinalizeAfterMinHookShutdown() noexcept
{
	created_ = false;
	enabled_ = false;
	gEndpoint.store(nullptr, std::memory_order_release);
}

std::uint32_t GalaxyPeerEndpoint::PublicIpv4NetworkOrder() const noexcept
{
	return publicIpv4_.load(std::memory_order_acquire);
}

void GalaxyPeerEndpoint::Observe(void *rawRakPeer, const GalaxySystemAddress *result) noexcept
{
	if (!transport_ || !transport_->Armed()) return;
	std::uint32_t validationError{};
	if (!result) validationError |= 1u << 0;
	std::uintptr_t vtable{};
	if (rawRakPeer) vtable = *reinterpret_cast<const std::uintptr_t *>(rawRakPeer);
	if (vtable != expectedVtable_) validationError |= 1u << 1;
	if (!result || result->family != AF_INET) validationError |= 1u << 2;
	if (!result || !result->addressNetworkOrder) validationError |= 1u << 3;
	if (!result || !IsPublicIpv4(result->addressNetworkOrder)) validationError |= 1u << 4;
	if (!gamePort_) validationError |= 1u << 5;

	const auto valid = validationError == 0;
	std::uint32_t publicIpv4{};
	if (valid)
	{
		publicIpv4 = result->addressNetworkOrder;
		std::uint32_t expected{};
		publicIpv4_.compare_exchange_strong(expected, publicIpv4, std::memory_order_acq_rel);
	}
	const auto address = result && result->family == AF_INET ? result->addressNetworkOrder : 0;
	const auto portUsable =
		result && result->family == AF_INET && gamePort_ && ntohs(result->portNetworkOrder) == gamePort_;
	transport_->ReportEndpointObservation(AddressClass(address), portUsable, valid && portUsable);
}

#if defined(BF2_DIRECT_ABI_TEST)
void GalaxyPeerEndpoint::ConfigureAbiTest(ServerTransport &transport, void *original, std::uintptr_t expectedVtable,
										  std::uint16_t gamePort) noexcept
{
	Prepare(transport, gamePort);
	expectedVtable_ = expectedVtable;
	gGetExternalId = reinterpret_cast<GetExternalIdFn>(original);
	gEndpoint.store(this, std::memory_order_release);
}

void *GalaxyPeerEndpoint::HookForAbiTest() noexcept
{
	return reinterpret_cast<void *>(&HookGetExternalId);
}
#endif

} // namespace bf2direct
