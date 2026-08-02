#pragma once

#include "direct_transport/direct_transport_profile.h"

#include <bf2direct/galaxy.h>
#include <bf2direct/security.h>
#include <bf2direct/socket.h>
#include <bf2direct/support.h>

#include <array>
#include <atomic>
#include <cstddef>
#include <cstdint>
#include <span>
#include <string>

namespace bf2direct
{

class GalaxyPeerEndpoint;
class ServerHooks;

class ServerTransport
{
public:
	bool Initialize(Policy policy, std::string &error) noexcept;
	bool Shutdown(std::string &error) noexcept;
	void Tick() noexcept;
	void Arm() noexcept;
	void Disarm() noexcept;
	bool Armed() const noexcept;
	bool InitializeSocket(std::string &warning) noexcept;
	void ShutdownSocket() noexcept;
	void BeforeReceive() noexcept;
	void AfterReceive() noexcept;
	void OnNativeTransmit(int physicalPrimary) noexcept;
	int BeginTransmitGroup(int physicalPrimary) noexcept;
	void EndTransmitGroup(int physicalPrimary) noexcept;
	NativeTransmitResult TransmitNative(int physicalPrimary, int groupPrimary,
										std::span<const std::uint8_t> bytes) noexcept;
	void OnNativeIntake(void *endpoint) noexcept;
	void OnNativeDisconnect(int physicalPrimary) noexcept;
	void OnNativeDisconnectComplete(int physicalPrimary) noexcept;
	void OnReset(std::uint8_t mode) noexcept;
	void OnRemoteMember(const void *memberId, std::uint32_t state) noexcept;
	void OnLocalLobbyLeft() noexcept;
	void ReportEndpointObservation(EndpointAddressClass addressClass, bool portUsable, bool usable) noexcept;
	void ReportEndpointObserverFailure(const char *reason, std::uint32_t error = 0) noexcept;

private:
	friend struct ServerTransportLoggingTestAccess;
	friend struct ServerTransportTestAccess;

	struct Association
	{
		std::uint64_t galaxyId{};
		std::uint64_t clientNonce{};
		std::uint64_t phaseStartMs{};
		std::uint64_t proofDeadlineMs{};
		std::uint32_t connectionId{};
		std::uint32_t generation{};
		std::uint32_t sendSequence{};
		SessionKey sessionKey{};
		Endpoint provisionalEndpoint{};
		Endpoint committedEndpoint{};
		ReplayWindow receiveReplay;
		RouteState state{RouteState::Unclassified};
		TransmitRoute transmitRoute{TransmitRoute::Galaxy};
		Carrier groupCarrier{Carrier::Galaxy};
		std::uint32_t groupGeneration{};
		std::uint32_t groupDepth{};
		std::uint8_t offerAttempts{};
		std::uint8_t commitAttempts{};
		std::uint8_t controlMessagesSeen{};
		bool live{};
		bool offerSubmitted{};
		bool commitStarted{};
		bool receivePermission{};
		PeerClassification classification{PeerClassification::Unknown};
		RemovalAction removalAction{RemovalAction::None};
#if defined(_DEBUG)
		bool terminalRouteLogged{};
		std::uint64_t startMs{};
		std::uint64_t directStartMs{};
		HandshakeMilestone lastMilestone{HandshakeMilestone::None};
		AssociationSupportCounters support;
		std::uint8_t peerProtocol{};
		bool enteredDirect{};
#endif
	};

#if defined(_DEBUG)
	enum class SupportSocketOperation : std::uint8_t
	{
		Send,
		Receive,
	};

	struct SocketErrorEntry
	{
		std::uint64_t occurrences{};
		std::uint32_t error{};
		SupportSocketOperation operation{SupportSocketOperation::Send};
		bool used{};
	};
#endif

	enum class ServerInvariant : std::uint8_t
	{
		StaleGenerationOutput = 0,
		GroupDepthUnderflow = 1,
		GroupDepthOverflow = 2,
		ThreadViolation = 4,
		IllegalRouteTransition = 5,
		PostDirectGalaxySubmission = 6,
		IncompleteRollback = 7,
		IncompleteShutdown = 8,
		Count = 9,
	};

	GalaxyNetworking Networking() const noexcept;
	bool OnNetworkThread(bool claim = false) noexcept;
	bool ReadIdentity(std::uint8_t physicalPrimary, std::uint64_t &identity) const noexcept;
	int ResolvePhysicalPrimary(int destination) const noexcept;
	int FindIdentity(std::uint64_t identity) const noexcept;
	int FindEndpoint(const void *endpoint) const noexcept;
	int FindConnection(std::uint32_t connectionId) const noexcept;
	void StartAssociation(std::uint8_t physicalPrimary, std::uint64_t identity, std::uint64_t now) noexcept;
	void InvalidateAssociation(std::uint8_t physicalPrimary, LifecycleReason kind) noexcept;
	void InvalidateAll(LifecycleReason kind) noexcept;
	void PumpControl(std::uint64_t now) noexcept;
	void PumpDirect() noexcept;
	void ServiceAssociations(std::uint64_t now) noexcept;
	void HandleControl(std::uint8_t physicalPrimary, const ParsedControl &control, ParseStatus status,
					   std::uint64_t now) noexcept;
	void HandleDirect(std::span<const std::uint8_t> bytes, const Endpoint &source) noexcept;
	void BeginOffer(std::uint8_t physicalPrimary, std::uint64_t nonce, std::uint64_t now) noexcept;
	bool SendControl(std::uint8_t physicalPrimary, ControlKind kind, std::span<const std::uint8_t> bytes) noexcept;
	void SendProbeAck(std::uint8_t physicalPrimary, const Endpoint &destination) noexcept;
	void DirectFailure(std::uint8_t physicalPrimary, RouteReason reason) noexcept;
	void ServiceRemovals() noexcept;
	void RemovePeer(std::uint8_t physicalPrimary, RouteReason reason) noexcept;
	void SetState(std::uint8_t physicalPrimary, RouteState next, RouteReason reason) noexcept;
	void SetRoute(std::uint8_t physicalPrimary, RouteState next, TransmitRoute transmitRoute, bool receivePermission,
				  RouteReason reason) noexcept;
	void CompleteMilestone(Association &association, HandshakeMilestone milestone) noexcept;
	void LogStartup() const noexcept;
	void LogTerminalRoute(std::uint8_t physicalPrimary, RouteReason reason, std::uint64_t now,
						  RemovalAction removal = RemovalAction::None) noexcept;
	void LogAssociationEnd(std::uint8_t physicalPrimary, LifecycleReason reason, std::uint64_t now) noexcept;
#if defined(_DEBUG)
	void LogSocketError(SupportSocketOperation operation, std::uint32_t error) noexcept;
#endif
	void RecordOverflow(std::uint32_t discarded) noexcept;
	void RecordInvariant(ServerInvariant invariant, std::uint8_t physicalPrimary = 0xff, std::uint32_t error = 0,
						 std::uint32_t value0 = 0, std::uint32_t value1 = 0) noexcept;
	void LogInvariant(ServerInvariant invariant, std::uint64_t occurrence, std::uint8_t physicalPrimary,
					  std::uint32_t error, std::uint32_t value0, std::uint32_t value1) noexcept;

	std::atomic_bool armed_{};
	ServerHooks *hooks_{};
	GalaxyPeerEndpoint *endpoint_{};
	HMODULE executable_{};
	const ServerProfile *profile_{};
	Policy policy_{Policy::Disabled};
	std::uint16_t gamePort_{};
	std::array<Association, 64> associations_{};
	std::array<bool, 64> rearmBlocked_{};
	std::array<int, 64> pendingRemovals_{};
	bool hasPendingRemovals_{};
	std::uint32_t nextGeneration_{};
	std::atomic_uint32_t networkThreadId_{};
	std::uint64_t nextControlPumpMs_{};
	std::array<std::uint8_t, 64> controlBuffer_{};
	UdpSocketRuntime socket_;
	std::array<std::uint8_t, kMaximumDirectDatagramBytes> receiveBuffer_;
	std::array<std::uint8_t, kMaximumDirectDatagramBytes> sendBuffer_;
#if defined(_DEBUG)
	std::array<SocketErrorEntry, 8> socketErrors_{};
	bool socketErrorTableSaturated_{};
#endif
	std::array<std::uint64_t, static_cast<std::size_t>(ServerInvariant::Count)> invariantCounts_{};
	std::atomic_uint32_t endpointSupportState_{};
	std::uint64_t overflowDiscards_{};
	std::uint64_t overflowOccurrences_{};
#if defined(BF2_DIRECT_SERVER_TEST)
	GalaxyNetworking testNetworking_;
#endif
};

ServerTransport &GetServerTransport() noexcept;

} // namespace bf2direct
