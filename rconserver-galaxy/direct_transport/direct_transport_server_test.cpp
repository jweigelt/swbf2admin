#include "direct_transport/direct_transport_server.h"
#include "direct_transport/galaxy_peer_endpoint.h"

#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <span>
#include <string>

namespace bf2direct
{

struct SendState
{
	std::array<std::uint64_t, 2> peers{};
	std::uint32_t count{};
	bool valid{true};
};

bool Send(void *context, std::uint64_t peer, const void *bytes, std::uint32_t length,
		  std::uint32_t sendType, std::uint8_t channel) noexcept
{
	auto &state = *static_cast<SendState *>(context);
	ParsedControl control{};
	const auto parsed = ParseControl(
		std::span<const std::uint8_t>(static_cast<const std::uint8_t *>(bytes), length), control);
	if (state.count >= state.peers.size() || parsed != ParseStatus::Ok || control.kind != ControlKind::Commit ||
		sendType != kGalaxyReliableImmediate || channel != kControlChannel)
	{
		state.valid = false;
		return false;
	}
	state.peers[state.count++] = peer;
	return true;
}

bool Available(void *, std::uint32_t *, std::uint8_t) noexcept
{
	return false;
}

bool Read(void *, void *, std::uint32_t, std::uint32_t *, std::uint64_t *, std::uint8_t) noexcept
{
	return false;
}

void Pop(void *, std::uint8_t) noexcept
{
}

struct ServerTransportTestAccess
{
	static void Prepare(ServerTransport &transport, ServerProfile &profile, std::span<std::byte> image,
						SendState &sendState) noexcept
	{
		profile = {};
		profile.endpointTableRva = 0x100;
		transport.executable_ = reinterpret_cast<HMODULE>(image.data());
		transport.profile_ = &profile;
		transport.associations_ = {};
		transport.pendingRemovals_.fill(-1);
		transport.hasPendingRemovals_ = false;
		transport.testNetworking_ = GalaxyNetworking({&sendState, &Send, &Available, &Read, &Pop});
	}

	static void PrepareCommit(ServerTransport &transport, const ServerProfile &profile, std::span<std::byte> image,
						  std::uint8_t slot, std::uint64_t identity, std::uint32_t connectionId,
						  std::uint64_t now) noexcept
	{
		constexpr std::uint32_t endpointStride = 0x60;
		constexpr std::uint32_t connectedOffset = 0x20;
		auto *record = image.data() + profile.endpointTableRva + slot * endpointStride;
		*(record - connectedOffset) = std::byte{1};
		std::memcpy(record, &identity, sizeof(identity));

		auto &association = transport.associations_[slot];
		association = {};
		association.live = true;
		association.galaxyId = identity;
		association.connectionId = connectionId;
		association.phaseStartMs = now;
		association.provisionalEndpoint = {0x0100007f, static_cast<std::uint16_t>(40000 + slot)};
		association.state = RouteState::AwaitCommit;
		association.commitStarted = true;
	}

	static void Service(ServerTransport &transport, std::uint64_t now) noexcept
	{
		transport.ServiceAssociations(now);
	}

	static bool Committed(const ServerTransport &transport, std::uint8_t slot) noexcept
	{
		const auto &association = transport.associations_[slot];
		return association.state == RouteState::DirectLocked &&
			association.transmitRoute == TransmitRoute::DirectPending && association.receivePermission &&
			association.committedEndpoint == association.provisionalEndpoint;
	}
};

struct GalaxyPeerEndpointTestAccess
{
	static void PrepareFailedRollback(GalaxyPeerEndpoint &endpoint, ServerTransport &transport) noexcept
	{
		endpoint.Prepare(transport, 3658);
		endpoint.target_ = reinterpret_cast<void *>(1);
		endpoint.created_ = true;
		endpoint.enabled_ = true;
	}

	static bool Recovered(const GalaxyPeerEndpoint &endpoint) noexcept
	{
		return !endpoint.created_ && !endpoint.enabled_;
	}
};

} // namespace bf2direct

int main()
{
	using namespace bf2direct;

	std::array<std::byte, 0x200> image{};
	ServerProfile profile{};
	SendState sendState{};
	ServerTransport transport;
	ServerTransportTestAccess::Prepare(transport, profile, image, sendState);
	ServerTransportTestAccess::PrepareCommit(transport, profile, image, 0, 0x101, 0x1001, 1000);
	ServerTransportTestAccess::PrepareCommit(transport, profile, image, 1, 0x202, 0x2002, 1000);
	ServerTransportTestAccess::Service(transport, 1000);
	if (!sendState.valid || sendState.count != 2 || sendState.peers[0] != 0x101 || sendState.peers[1] != 0x202 ||
		!ServerTransportTestAccess::Committed(transport, 0) || !ServerTransportTestAccess::Committed(transport, 1))
	{
		return 1;
	}

	auto &endpoint = GetGalaxyPeerEndpoint();
	GalaxyPeerEndpointTestAccess::PrepareFailedRollback(endpoint, transport);
	std::string error;
	if (endpoint.Rollback(error) || error.empty()) return 2;
	endpoint.FinalizeAfterMinHookShutdown();
	if (!GalaxyPeerEndpointTestAccess::Recovered(endpoint)) return 3;
	return 0;
}
