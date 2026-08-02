#include "direct_transport/direct_transport_server.h"

#include "direct_transport/direct_transport_hooks.h"
#include "direct_transport/galaxy_peer_endpoint.h"

#include <bf2direct/data.h>
#include <bf2direct/native.h>

#include <WinSock2.h>
#include <Windows.h>

#include <array>
#include <cstring>
#include <limits>

namespace bf2direct
{
namespace
{

constexpr std::uint64_t kOfferSafetyTimeoutMs = 10000;

} // namespace

ServerTransport &GetServerTransport() noexcept
{
	static ServerTransport transport;
	return transport;
}

bool ServerTransport::Initialize(Policy policy, std::string &error) noexcept
{
	policy_ = policy;
	pendingRemovals_.fill(-1);
	hasPendingRemovals_ = false;
#if defined(_DEBUG)
	socketErrors_ = {};
	socketErrorTableSaturated_ = false;
#endif
	invariantCounts_ = {};
	endpointSupportState_.store(0, std::memory_order_release);
	overflowDiscards_ = 0;
	overflowOccurrences_ = 0;
	executable_ = GetModuleHandleW(L"BattlefrontII.exe");
	profile_ = &GetServerProfile();
	if (!VerifyServerProfile(executable_, *profile_, error)) return false;
	const auto gamePort = *reinterpret_cast<const std::uint32_t *>(reinterpret_cast<const std::byte *>(executable_) +
																   profile_->gamePortRva);
	gamePort_ = gamePort > 0 && gamePort <= 0xffff ? static_cast<std::uint16_t>(gamePort) : 0;
	std::string socketWarning;
	InitializeSocket(socketWarning);
	endpoint_ = &GetGalaxyPeerEndpoint();
	endpoint_->Prepare(*this, gamePort_);

	hooks_ = &GetServerHooks();
	if (!hooks_->Prepare(executable_, *profile_, *this, error) || !hooks_->Install(error))
	{
		auto installError = error;
		const auto failedHook = hooks_->FailedHook();
		if (static_cast<std::uint8_t>(failedHook))
		{
			installError += "; hook=";
			installError += HookIdName(failedHook);
			installError += " installed=" + std::to_string(hooks_->InstalledCount());
			installError += " expected=" + std::to_string(hooks_->ExpectedCount());
			installError += " error=" + std::to_string(hooks_->FailureError());
		}
		std::string rollbackError;
		const auto rollbackComplete = hooks_->Rollback(rollbackError);
		if (!rollbackComplete) RecordInvariant(ServerInvariant::IncompleteRollback);
		ShutdownSocket();
		hooks_ = nullptr;
		endpoint_ = nullptr;
		error = installError;
		if (!rollbackError.empty()) error += "; rollback failed: " + rollbackError;
		return false;
	}
	Arm();
	LogStartup();
	endpoint_->TryInstall();
	if (!socketWarning.empty())
	{
		if (!error.empty()) error += "; ";
		error += socketWarning;
	}
	return true;
}

bool ServerTransport::Shutdown(std::string &error) noexcept
{
	Disarm();
	std::string endpointError;
	if (endpoint_) endpoint_->Rollback(endpointError);
	std::string hookError;
	const auto hookComplete = !hooks_ || hooks_->Rollback(hookError);
	if (hookComplete && endpoint_)
	{
		// MinHook shutdown owns any endpoint hook its targeted rollback could not remove.
		endpoint_->FinalizeAfterMinHookShutdown();
	}
	const auto complete = hookComplete;
	if (!complete)
	{
		error = endpointError;
		if (!hookError.empty())
		{
			if (!error.empty()) error += "; ";
			error += hookError;
		}
	}
	ShutdownSocket();
	const auto now = GetTickCount64();
	for (std::uint8_t physicalPrimary{}; physicalPrimary < associations_.size(); ++physicalPrimary)
	{
		if (associations_[physicalPrimary].live)
		{
			LogAssociationEnd(physicalPrimary, LifecycleReason::Shutdown, now);
		}
	}
	if (!complete || !endpointError.empty()) RecordInvariant(ServerInvariant::IncompleteShutdown);
	if (complete)
	{
		hooks_ = nullptr;
		endpoint_ = nullptr;
	}
	return complete;
}

void ServerTransport::Tick() noexcept
{
	if (endpoint_) endpoint_->TryInstall();
}

void ServerTransport::Arm() noexcept
{
	armed_.store(true, std::memory_order_release);
}

void ServerTransport::Disarm() noexcept
{
	armed_.store(false, std::memory_order_release);
}

bool ServerTransport::Armed() const noexcept
{
	return armed_.load(std::memory_order_acquire);
}

bool ServerTransport::InitializeSocket(std::string &warning) noexcept
{
	if (!gamePort_)
	{
		warning = "Direct transport socket unavailable: invalid /gameport";
		return false;
	}
	std::int32_t error{};
	const auto available = socket_.Start(gamePort_, error);
	if (!available)
	{
		warning = "Direct transport socket unavailable: " + std::to_string(error);
	}
	return available;
}

void ServerTransport::ShutdownSocket() noexcept
{
	socket_.Stop();
}

GalaxyNetworking ServerTransport::Networking() const noexcept
{
#if defined(BF2_DIRECT_SERVER_TEST)
	if (testNetworking_.Valid()) return testNetworking_;
#endif
	if (!executable_ || !profile_) return GalaxyNetworking{};
	using GetNetworkingFn = void *(__cdecl *)();
	const auto function =
		reinterpret_cast<GetNetworkingFn>(reinterpret_cast<std::byte *>(executable_) + profile_->getNetworkingRva);
	return GalaxyNetworking::FromInterface(function());
}

bool ServerTransport::OnNetworkThread(bool claim) noexcept
{
	const auto current = GetCurrentThreadId();
	auto expected = networkThreadId_.load(std::memory_order_acquire);
	if (!expected && claim)
	{
		networkThreadId_.compare_exchange_strong(expected, current, std::memory_order_acq_rel,
												 std::memory_order_acquire);
		if (!expected) expected = current;
	}
	if (!expected || expected == current) return true;
	RecordInvariant(ServerInvariant::ThreadViolation, 0xff, 0, expected, current);
	return false;
}

bool ServerTransport::ReadIdentity(std::uint8_t physicalPrimary, std::uint64_t &identity) const noexcept
{
	if (!executable_ || !profile_) return false;
	const auto *endpointTable = reinterpret_cast<const std::byte *>(executable_) + profile_->endpointTableRva;
	return ReadNativeIdentity(endpointTable, physicalPrimary, identity);
}

int ServerTransport::ResolvePhysicalPrimary(int destination) const noexcept
{
	if (destination < 0 || destination >= 67) return -1;
	std::uint64_t identity{};
	if (!ReadIdentity(static_cast<std::uint8_t>(destination), identity)) return -1;
	return FindIdentity(identity);
}

int ServerTransport::FindIdentity(std::uint64_t identity) const noexcept
{
	if (!identity) return -1;
	for (std::uint8_t physicalPrimary{}; physicalPrimary < associations_.size(); ++physicalPrimary)
	{
		std::uint64_t current{};
		if (ReadIdentity(physicalPrimary, current) && current == identity) return physicalPrimary;
	}
	return -1;
}

int ServerTransport::FindEndpoint(const void *endpoint) const noexcept
{
	if (!endpoint) return -1;
	std::uint64_t identity{};
	std::memcpy(&identity, endpoint, sizeof(identity));
	return FindIdentity(identity);
}

int ServerTransport::FindConnection(std::uint32_t connectionId) const noexcept
{
	if (!connectionId) return -1;
	for (std::uint8_t physicalPrimary{}; physicalPrimary < associations_.size(); ++physicalPrimary)
	{
		const auto &association = associations_[physicalPrimary];
		if (association.live && association.connectionId == connectionId) return physicalPrimary;
	}
	return -1;
}

void ServerTransport::BeforeReceive() noexcept
{
	if (!Armed() || !profile_) return;
	if (!OnNetworkThread(true)) return;
	const auto now = GetTickCount64();
	PumpControl(now);
	PumpDirect();
}

void ServerTransport::AfterReceive() noexcept
{
	if (!Armed() || !OnNetworkThread()) return;
	ServiceAssociations(GetTickCount64());
}

void ServerTransport::OnNativeTransmit(int physicalPrimary) noexcept
{
	if (!Armed() || !profile_ || !OnNetworkThread()) return;
	const auto resolved = ResolvePhysicalPrimary(physicalPrimary);
	if (resolved < 0 || resolved >= static_cast<int>(associations_.size())) return;
	physicalPrimary = resolved;
	if (rearmBlocked_[physicalPrimary]) return;
	std::uint64_t identity{};
	if (!ReadIdentity(static_cast<std::uint8_t>(physicalPrimary), identity)) return;
	auto &association = associations_[physicalPrimary];
	if (association.live && association.galaxyId == identity) return;
	if (association.live)
	{
		InvalidateAssociation(static_cast<std::uint8_t>(physicalPrimary), LifecycleReason::SlotReuse);
	}
	StartAssociation(static_cast<std::uint8_t>(physicalPrimary), identity, GetTickCount64());
}

int ServerTransport::BeginTransmitGroup(int destination) noexcept
{
	const auto resolved = ResolvePhysicalPrimary(destination);
	if (resolved < 0 || resolved >= static_cast<int>(associations_.size())) return -1;
	auto &association = associations_[resolved];
	if (!association.live) return -1;
	if (!association.groupDepth)
	{
		if (association.transmitRoute == TransmitRoute::DirectPending)
		{
			SetRoute(static_cast<std::uint8_t>(resolved), association.state, TransmitRoute::Direct,
					 association.receivePermission, RouteReason::Commit);
		}
		association.groupCarrier =
			association.transmitRoute == TransmitRoute::Direct ? Carrier::Direct : Carrier::Galaxy;
		association.groupGeneration = association.generation;
	}
	if (association.groupDepth == (std::numeric_limits<std::uint32_t>::max)())
	{
		RecordInvariant(ServerInvariant::GroupDepthOverflow, static_cast<std::uint8_t>(resolved));
		return -1;
	}
	++association.groupDepth;
	return resolved;
}

void ServerTransport::EndTransmitGroup(int physicalPrimary) noexcept
{
	if (physicalPrimary < 0 || physicalPrimary >= static_cast<int>(associations_.size())) return;
	auto &association = associations_[physicalPrimary];
	if (!association.groupDepth)
	{
		RecordInvariant(ServerInvariant::GroupDepthUnderflow, static_cast<std::uint8_t>(physicalPrimary));
		return;
	}
	if (--association.groupDepth) return;
	association.groupCarrier = Carrier::Galaxy;
	association.groupGeneration = 0;
}

NativeTransmitResult ServerTransport::TransmitNative(int destination, int groupPrimary,
													 std::span<const std::uint8_t> bytes) noexcept
{
	NativeTransmitResult output{};
	auto resolved = groupPrimary >= 0 ? groupPrimary : ResolvePhysicalPrimary(destination);
	if (resolved < 0 || resolved >= static_cast<int>(associations_.size())) return output;
	auto &association = associations_[resolved];
	const auto grouped = association.groupDepth != 0;
	if (!association.live)
	{
		if (grouped)
		{
			output.handled = true;
			output.result = -1;
			output.error = WSAENOTCONN;
			RecordInvariant(ServerInvariant::StaleGenerationOutput, static_cast<std::uint8_t>(resolved), WSAENOTCONN, 0,
							association.groupGeneration);
		}
		return output;
	}
	std::uint64_t identity{};
	if (!ReadIdentity(static_cast<std::uint8_t>(resolved), identity) || identity != association.galaxyId ||
		(grouped && association.groupGeneration != association.generation))
	{
		if (grouped || association.transmitRoute != TransmitRoute::Galaxy)
		{
			output.handled = true;
			output.result = -1;
			output.error = WSAENOTCONN;
			RecordInvariant(ServerInvariant::StaleGenerationOutput, static_cast<std::uint8_t>(resolved), WSAENOTCONN,
							association.generation, association.groupGeneration);
		}
		return output;
	}
	auto carrier = grouped ? association.groupCarrier : Carrier::Galaxy;
	if (!grouped)
	{
		if (association.transmitRoute == TransmitRoute::DirectPending)
		{
			SetRoute(static_cast<std::uint8_t>(resolved), association.state, TransmitRoute::Direct,
					 association.receivePermission, RouteReason::Commit);
		}
		if (association.transmitRoute == TransmitRoute::Direct) carrier = Carrier::Direct;
	}
	if (carrier != Carrier::Direct)
	{
		if (association.state == RouteState::DirectLocked)
		{
			RecordInvariant(ServerInvariant::PostDirectGalaxySubmission, static_cast<std::uint8_t>(resolved));
		}
		return output;
	}
	output.handled = true;
	if (bytes.size() < kNativeHeaderBytes || bytes.size() > kMaximumNativeBytes)
	{
		output.result = -1;
		output.error = WSAEMSGSIZE;
		return output;
	}
	const auto sequence = association.sendSequence++;
	if (association.state != RouteState::DirectLocked || !association.committedEndpoint.Valid() || !socket_.Available())
	{
		output.result = -1;
		output.error = WSAENOTCONN;
		RecordInvariant(ServerInvariant::StaleGenerationOutput, static_cast<std::uint8_t>(resolved), WSAENOTCONN,
						association.generation, association.groupGeneration);
#if defined(_DEBUG)
		++association.support.lifecycleRejects;
#endif
		return output;
	}
	output = SendDirectData(socket_, sendBuffer_, Direction::ServerToClient, association.sessionKey,
							association.connectionId, sequence, association.committedEndpoint, bytes);
	const auto complete = output.result == static_cast<std::int32_t>(bytes.size());
	if (!complete)
	{
#if defined(_DEBUG)
		LogSocketError(SupportSocketOperation::Send, static_cast<std::uint32_t>(output.error));
		++association.support.sendErrors;
#endif
	}
	else
	{
#if defined(_DEBUG)
		++association.support.transmitDatagrams;
		association.support.transmitBytes += kDirectHeaderBytes + bytes.size();
#endif
	}
	return output;
}

void ServerTransport::OnNativeIntake(void *endpoint) noexcept
{
	if (!Armed() || !profile_ || !OnNetworkThread()) return;
	const auto physicalPrimary = FindEndpoint(endpoint);
	if (physicalPrimary < 0 || physicalPrimary >= static_cast<int>(associations_.size())) return;
	if (rearmBlocked_[physicalPrimary]) return;
	std::uint64_t identity{};
	if (!ReadIdentity(static_cast<std::uint8_t>(physicalPrimary), identity)) return;
	auto &association = associations_[physicalPrimary];
	if (association.live && association.galaxyId == identity) return;
	if (association.live)
	{
		InvalidateAssociation(static_cast<std::uint8_t>(physicalPrimary), LifecycleReason::SlotReuse);
	}
	StartAssociation(static_cast<std::uint8_t>(physicalPrimary), identity, GetTickCount64());
}

void ServerTransport::OnNativeDisconnect(int physicalPrimary) noexcept
{
	if (!OnNetworkThread()) return;
	if (physicalPrimary >= 0 && physicalPrimary < static_cast<int>(associations_.size()))
	{
		rearmBlocked_[physicalPrimary] = true;
		InvalidateAssociation(static_cast<std::uint8_t>(physicalPrimary), LifecycleReason::NativeDisconnect);
	}
}

void ServerTransport::OnNativeDisconnectComplete(int physicalPrimary) noexcept
{
	if (!OnNetworkThread()) return;
	if (physicalPrimary >= 0 && physicalPrimary < static_cast<int>(rearmBlocked_.size()))
	{
		rearmBlocked_[physicalPrimary] = false;
	}
}

void ServerTransport::OnReset(std::uint8_t mode) noexcept
{
	if (!OnNetworkThread()) return;
	if (mode != 1)
	{
		InvalidateAll(LifecycleReason::DestructiveReset);
		pendingRemovals_.fill(-1);
		hasPendingRemovals_ = false;
	}
}

void ServerTransport::OnRemoteMember(const void *memberId, std::uint32_t state) noexcept
{
	constexpr std::uint32_t departure = 0x02 | 0x04 | 0x08 | 0x10;
	if (!profile_ || !memberId || !(state & departure) || !OnNetworkThread()) return;
	std::uint64_t identity{};
	std::memcpy(&identity, memberId, sizeof(identity));
	const auto physicalPrimary = FindIdentity(identity);
	if (physicalPrimary >= 0)
	{
		rearmBlocked_[physicalPrimary] = true;
		InvalidateAssociation(static_cast<std::uint8_t>(physicalPrimary), LifecycleReason::RemoteMemberLeft);
	}
}

void ServerTransport::OnLocalLobbyLeft() noexcept
{
	if (!OnNetworkThread()) return;
	InvalidateAll(LifecycleReason::LocalLobbyLeft);
	pendingRemovals_.fill(-1);
	hasPendingRemovals_ = false;
}

void ServerTransport::StartAssociation(std::uint8_t physicalPrimary, std::uint64_t identity, std::uint64_t now) noexcept
{
	Association next{};
	next.groupCarrier = associations_[physicalPrimary].groupCarrier;
	next.groupGeneration = associations_[physicalPrimary].groupGeneration;
	next.groupDepth = associations_[physicalPrimary].groupDepth;
	next.live = true;
	next.galaxyId = identity;
	next.phaseStartMs = now;
	if (++nextGeneration_ == 0) ++nextGeneration_;
	next.generation = nextGeneration_;
#if defined(_DEBUG)
	next.startMs = now;
#endif
	associations_[physicalPrimary] = next;
}

void ServerTransport::InvalidateAssociation(std::uint8_t physicalPrimary, LifecycleReason kind) noexcept
{
	if (physicalPrimary >= associations_.size()) return;
	auto &association = associations_[physicalPrimary];
	if (!association.live) return;
	LogAssociationEnd(physicalPrimary, kind, GetTickCount64());
	const auto groupCarrier = association.groupCarrier;
	const auto groupGeneration = association.groupGeneration;
	const auto groupDepth = association.groupDepth;
	association = {};
	association.groupCarrier = groupCarrier;
	association.groupGeneration = groupGeneration;
	association.groupDepth = groupDepth;
}

void ServerTransport::InvalidateAll(LifecycleReason kind) noexcept
{
	for (std::uint8_t physicalPrimary{}; physicalPrimary < associations_.size(); ++physicalPrimary)
	{
		InvalidateAssociation(physicalPrimary, kind);
	}
}

void ServerTransport::PumpControl(std::uint64_t now) noexcept
{
	if (now < nextControlPumpMs_) return;
	nextControlPumpMs_ = now + kControlPumpIntervalMs;
	const auto networking = Networking();
	if (!networking.Valid()) return;
	std::uint32_t consumed{};
	while (consumed < kControlDrainLimit)
	{
		std::uint32_t reported{};
		if (!networking.IsPacketAvailable(reported, kControlChannel)) break;
		++consumed;
		if (reported > controlBuffer_.size())
		{
			networking.PopPacket(kControlChannel);
			continue;
		}
		std::uint32_t bytesRead{};
		std::uint64_t sender{};
		if (!networking.ReadPacket(controlBuffer_, bytesRead, sender, kControlChannel))
		{
			break;
		}
		const auto physicalPrimary = FindIdentity(sender);
		if (physicalPrimary < 0 || !associations_[physicalPrimary].live ||
			associations_[physicalPrimary].galaxyId != sender)
		{
			continue;
		}
		ParsedControl control{};
		const auto status = ParseControl(std::span<const std::uint8_t>(controlBuffer_).first(bytesRead), control);
		HandleControl(static_cast<std::uint8_t>(physicalPrimary), control, status, now);
	}
}

void ServerTransport::PumpDirect() noexcept
{
	if (!socket_.Available()) return;
	std::uint32_t admitted{};
	std::uint32_t discarded{};
	for (;;)
	{
		Endpoint source{};
		const auto received = socket_.Receive(receiveBuffer_, source);
		if (!received.Succeeded())
		{
			if (received.error == WSAEWOULDBLOCK) break;
			if (received.error == WSAEMSGSIZE)
			{
				if (admitted++ >= kReceiveAdmissionLimit) ++discarded;
				continue;
			}
#if defined(_DEBUG)
			LogSocketError(SupportSocketOperation::Receive, static_cast<std::uint32_t>(received.error));
#endif
			break;
		}
		if (admitted++ >= kReceiveAdmissionLimit)
		{
			++discarded;
			continue;
		}
		HandleDirect(std::span<const std::uint8_t>(receiveBuffer_).first(static_cast<std::size_t>(received.value)),
					 source);
	}
	if (discarded) RecordOverflow(discarded);
}

void ServerTransport::HandleControl(std::uint8_t physicalPrimary, const ParsedControl &control, ParseStatus status,
									std::uint64_t now) noexcept
{
	auto &association = associations_[physicalPrimary];
	if (++association.controlMessagesSeen > kControlAssociationLimit)
	{
		RemovePeer(physicalPrimary, RouteReason::ControlAbuse);
		return;
	}
	if (status == ParseStatus::UnsupportedVersion && control.kind == ControlKind::Caps && control.version)
	{
		association.classification = PeerClassification::Incompatible;
#if defined(_DEBUG)
		association.peerProtocol = control.version;
#endif
		if (policy_ == Policy::PreferDirect)
		{
			SetState(physicalPrimary, RouteState::GalaxyLocked, RouteReason::ProtocolIncompatibility);
		}
		else
		{
			RemovePeer(physicalPrimary, RouteReason::ProtocolIncompatibility);
		}
		return;
	}
	if (status != ParseStatus::Ok)
	{
		return;
	}
	if (control.kind == ControlKind::Caps && association.state == RouteState::Unclassified)
	{
		association.classification = PeerClassification::Patched;
		CompleteMilestone(association, HandshakeMilestone::Caps);
		BeginOffer(physicalPrimary, control.clientNonce, now);
		return;
	}
	if (control.kind == ControlKind::Ready && association.state == RouteState::Negotiating &&
		association.offerSubmitted && association.provisionalEndpoint.Valid() &&
		control.connectionId == association.connectionId)
	{
		association.commitStarted = true;
		association.commitAttempts = 0;
		association.phaseStartMs = now;
		SetState(physicalPrimary, RouteState::AwaitCommit, RouteReason::ReadySubmission);
		CompleteMilestone(association, HandshakeMilestone::Ready);
		return;
	}
}

void ServerTransport::HandleDirect(std::span<const std::uint8_t> bytes, const Endpoint &source) noexcept
{
	ParsedDirect direct{};
	const auto status = ParseDirect(bytes, direct);
	if (status != ParseStatus::Ok) return;
	const auto found = FindConnection(direct.connectionId);
	if (found < 0) return;
	const auto physicalPrimary = static_cast<std::uint8_t>(found);
	auto &association = associations_[physicalPrimary];
	const auto validState = direct.kind == DirectKind::Probe
								? association.offerSubmitted && (association.state == RouteState::Negotiating ||
																 association.state == RouteState::AwaitCommit)
								: direct.kind == DirectKind::Data && association.receivePermission &&
									  association.state == RouteState::DirectLocked;
	if (!validState)
	{
#if defined(_DEBUG)
		if (direct.kind == DirectKind::Data) ++association.support.lifecycleRejects;
#endif
		return;
	}
	const auto expectedEndpoint =
		direct.kind == DirectKind::Data ? association.committedEndpoint : association.provisionalEndpoint;
	if (!EndpointAdmitsSource(expectedEndpoint, source, direct.kind == DirectKind::Probe))
	{
#if defined(_DEBUG)
		if (direct.kind == DirectKind::Data) ++association.support.endpointRejects;
#endif
		return;
	}
	if (!VerifyDirectAuthenticationTag(Direction::ClientToServer, association.sessionKey,
									   bytes.first(kDirectHeaderBytes), direct.payload))
	{
#if defined(_DEBUG)
		if (direct.kind == DirectKind::Data) ++association.support.authenticationRejects;
#endif
		return;
	}
	const auto replay = association.receiveReplay.Admit(direct.datagramSequence);
	if (replay == ReplayResult::Duplicate)
	{
#if defined(_DEBUG)
		if (direct.kind == DirectKind::Data) ++association.support.replayRejects;
#endif
		return;
	}
	if (replay == ReplayResult::Stale || replay == ReplayResult::InvalidJump)
	{
#if defined(_DEBUG)
		if (direct.kind == DirectKind::Data) ++association.support.replayRejects;
#endif
		return;
	}
	if (direct.kind == DirectKind::Probe)
	{
		if (!association.provisionalEndpoint.Valid()) association.provisionalEndpoint = source;
		CompleteMilestone(association, HandshakeMilestone::Probe);
		SendProbeAck(physicalPrimary, source);
	}
	else if (direct.kind == DirectKind::Data)
	{
		void *packet{};
		auto *base = reinterpret_cast<std::byte *>(executable_);
		const NativePacketFactory factory{
			reinterpret_cast<NativePacketAllocate>(base + profile_->packetAllocateRva),
			reinterpret_cast<NativePacketInitialize>(base + profile_->packetInitializeRva),
			reinterpret_cast<const std::uint32_t *>(base + profile_->packetPrefixBytesRva),
		};
		if (!BuildNativePacket(factory, direct.payload, packet))
		{
#if defined(_DEBUG)
			++association.support.lifecycleRejects;
#endif
			return;
		}
#if defined(_DEBUG)
		++association.support.receiveDatagrams;
		association.support.receiveBytes += bytes.size();
#endif
		SubmitDirectNative(packet, &association.galaxyId);
	}
}

void ServerTransport::BeginOffer(std::uint8_t physicalPrimary, std::uint64_t nonce, std::uint64_t now) noexcept
{
	auto &association = associations_[physicalPrimary];
	association.clientNonce = nonce;
	association.phaseStartMs = now;
	association.offerAttempts = 0;
	SetState(physicalPrimary, RouteState::Negotiating, RouteReason::ValidCapability);
	const auto publicIpv4 = endpoint_ ? endpoint_->PublicIpv4NetworkOrder() : 0;
	if (!socket_.Available())
	{
		DirectFailure(physicalPrimary, RouteReason::SocketUnavailable);
		return;
	}
	if (!publicIpv4 || !gamePort_)
	{
		DirectFailure(physicalPrimary, RouteReason::EndpointUnavailable);
		return;
	}
	std::array<std::uint32_t, 64> liveIds{};
	for (std::size_t index{}; index < associations_.size(); ++index)
	{
		if (associations_[index].live) liveIds[index] = associations_[index].connectionId;
	}
	if (!GenerateConnectionId(SystemRandomSource(), liveIds, association.connectionId) ||
		!FillRandom(SystemRandomSource(), association.sessionKey))
	{
		DirectFailure(physicalPrimary, RouteReason::PolicyLock);
	}
}

bool ServerTransport::SendControl(std::uint8_t physicalPrimary, ControlKind kind,
								  std::span<const std::uint8_t> bytes) noexcept
{
	auto &association = associations_[physicalPrimary];
	const auto sent = Networking().SendReliableImmediate(association.galaxyId, bytes, kControlChannel);
	if (sent)
	{
		CompleteMilestone(association,
						  kind == ControlKind::Offer ? HandshakeMilestone::Offer : HandshakeMilestone::Commit);
	}
	return sent;
}

void ServerTransport::SendProbeAck(std::uint8_t physicalPrimary, const Endpoint &destination) noexcept
{
	auto &association = associations_[physicalPrimary];
	std::array<std::uint8_t, kProbeDatagramBytes> datagram{};
	const auto sequence = association.sendSequence++;
	WriteDirectHeader(datagram, DirectKind::ProbeAck, association.connectionId, sequence, 0);
	const auto tag =
		ComputeDirectAuthenticationTag(Direction::ServerToClient, association.sessionKey,
									   std::span<const std::uint8_t>(datagram).first(kDirectHeaderBytes),
									   std::span<const std::uint8_t>(datagram).subspan(kDirectHeaderBytes));
	StoreBigEndian64(datagram.data() + offsetof(DirectHeaderV1, authenticationTag), tag);
	const auto result = socket_.Send(datagram, destination);
	if (!result.Succeeded())
	{
#if defined(_DEBUG)
		LogSocketError(SupportSocketOperation::Send, static_cast<std::uint32_t>(result.error));
#endif
	}
	else
	{
		CompleteMilestone(association, HandshakeMilestone::Ack);
	}
}

void ServerTransport::ServiceAssociations(std::uint64_t now) noexcept
{
	for (std::uint8_t physicalPrimary{}; physicalPrimary < associations_.size(); ++physicalPrimary)
	{
		auto &association = associations_[physicalPrimary];
		if (!association.live) continue;
		std::uint64_t current{};
		if (!ReadIdentity(physicalPrimary, current) || current != association.galaxyId)
		{
			InvalidateAssociation(physicalPrimary, LifecycleReason::SlotReuse);
			continue;
		}
		if (association.state == RouteState::Unclassified)
		{
			if (now - association.phaseStartMs >= kHandshakeDeadlineMs)
			{
				association.classification = PeerClassification::Vanilla;
				if (policy_ == Policy::RequireDirectAll)
					RemovePeer(physicalPrimary, RouteReason::CapabilityTimeout);
				else
					SetState(physicalPrimary, RouteState::GalaxyLocked, RouteReason::CapabilityTimeout);
			}
			continue;
		}
		if (association.state == RouteState::Negotiating)
		{
			if (!association.connectionId) continue;
			if (!association.offerSubmitted)
			{
				if (now - association.phaseStartMs >= kHandshakeDeadlineMs)
				{
					DirectFailure(physicalPrimary, RouteReason::SubmissionTimeout);
					continue;
				}
				if (association.offerAttempts < kHandshakeAttemptLimit &&
					now - association.phaseStartMs >= association.offerAttempts * kHandshakeRetryIntervalMs)
				{
					std::array<std::uint8_t, kOfferBytes> offer{};
					WriteOffer(offer, association.clientNonce, association.connectionId,
							   endpoint_->PublicIpv4NetworkOrder(), gamePort_, association.sessionKey);
					++association.offerAttempts;
					association.offerSubmitted = SendControl(physicalPrimary, ControlKind::Offer, offer);
					if (association.offerSubmitted) association.proofDeadlineMs = now + kOfferSafetyTimeoutMs;
				}
			}
			else if (now >= association.proofDeadlineMs)
			{
				DirectFailure(physicalPrimary, RouteReason::ProofTimeout);
			}
			continue;
		}
		if (association.state == RouteState::AwaitCommit && association.commitStarted)
		{
			if (now - association.phaseStartMs >= kHandshakeDeadlineMs)
			{
				DirectFailure(physicalPrimary, RouteReason::SubmissionTimeout);
				continue;
			}
			if (association.commitAttempts < kHandshakeAttemptLimit &&
				now - association.phaseStartMs >= association.commitAttempts * kHandshakeRetryIntervalMs)
			{
				std::array<std::uint8_t, kActivationBytes> commit{};
				WriteActivation(commit, ControlKind::Commit, association.connectionId);
				++association.commitAttempts;
				if (SendControl(physicalPrimary, ControlKind::Commit, commit))
				{
					association.committedEndpoint = association.provisionalEndpoint;
					SetRoute(physicalPrimary, RouteState::DirectLocked, TransmitRoute::DirectPending, true,
							 RouteReason::Commit);
					continue;
				}
			}
		}
	}
	ServiceRemovals();
}

void ServerTransport::DirectFailure(std::uint8_t physicalPrimary, RouteReason reason) noexcept
{
	if (policy_ == Policy::PreferDirect)
	{
		auto &association = associations_[physicalPrimary];
		association.provisionalEndpoint = {};
		association.committedEndpoint = {};
		SetRoute(physicalPrimary, RouteState::GalaxyLocked, TransmitRoute::Galaxy, false, reason);
	}
	else
	{
		RemovePeer(physicalPrimary, reason);
	}
}

void ServerTransport::ServiceRemovals() noexcept
{
	if (!hasPendingRemovals_ || !executable_ || !profile_) return;
	using IsPlayingFn = bool(__cdecl *)(int, bool);
	using UpdateBootPlayerFn = void(__cdecl *)(int *);
	const auto *base = reinterpret_cast<std::byte *>(executable_);
	const auto isPlaying = reinterpret_cast<IsPlayingFn>(base + profile_->isPlayingRva);
	const auto updateBootPlayer = reinterpret_cast<UpdateBootPlayerFn>(base + profile_->updateBootPlayerRva);
	bool remaining{};
	for (auto &player : pendingRemovals_)
	{
		if (player < 0) continue;
		if (!isPlaying(player, false))
		{
			player = -1;
			continue;
		}
		updateBootPlayer(&player);
		if (player >= 0) remaining = true;
	}
	hasPendingRemovals_ = remaining;
}

void ServerTransport::RemovePeer(std::uint8_t physicalPrimary, RouteReason reason) noexcept
{
	if (physicalPrimary >= associations_.size() || !associations_[physicalPrimary].live) return;
	const auto action = reason == RouteReason::ControlAbuse	  ? RemovalAction::ControlAbuse
						: policy_ == Policy::RequireDirectAll ? RemovalAction::RequireDirectAll
															  : RemovalAction::RequireDirectPatched;
	LogTerminalRoute(physicalPrimary, reason, GetTickCount64(), action);
	rearmBlocked_[physicalPrimary] = true;
	InvalidateAssociation(physicalPrimary, LifecycleReason::ServerRemoval);
	using IsPlayingFn = bool(__cdecl *)(int, bool);
	using SetNotPlayingFn = void(__cdecl *)(int);
	const auto *base = reinterpret_cast<std::byte *>(executable_);
	const auto isPlaying = reinterpret_cast<IsPlayingFn>(base + profile_->isPlayingRva);
	if (isPlaying(physicalPrimary, false))
	{
		pendingRemovals_[physicalPrimary] = physicalPrimary;
		hasPendingRemovals_ = true;
		return;
	}
	const auto setNotPlaying = reinterpret_cast<SetNotPlayingFn>(base + profile_->setNotPlayingRva);
	setNotPlaying(physicalPrimary);
}

void ServerTransport::SetState(std::uint8_t physicalPrimary, RouteState next, RouteReason reason) noexcept
{
	auto &association = associations_[physicalPrimary];
	SetRoute(physicalPrimary, next, association.transmitRoute, association.receivePermission, reason);
}

void ServerTransport::SetRoute(std::uint8_t physicalPrimary, RouteState next, TransmitRoute transmitRoute,
							   bool receivePermission, RouteReason reason) noexcept
{
	auto &association = associations_[physicalPrimary];
	const auto previous = association.state;
	const auto previousRoute = association.transmitRoute;
	const auto previousReceivePermission = association.receivePermission;
	if (previous == next && previousRoute == transmitRoute && previousReceivePermission == receivePermission) return;
	if ((previous == RouteState::DirectLocked || previous == RouteState::GalaxyLocked) && previous != next)
	{
		RecordInvariant(ServerInvariant::IllegalRouteTransition, physicalPrimary, 0,
						static_cast<std::uint32_t>(previous), static_cast<std::uint32_t>(next));
	}
	association.state = next;
	association.transmitRoute = transmitRoute;
	association.receivePermission = receivePermission;
	const auto now = GetTickCount64();
#if defined(_DEBUG)
	if (next == RouteState::DirectLocked && previous != RouteState::DirectLocked)
	{
		association.enteredDirect = true;
		association.directStartMs = now;
	}
#endif
	if ((next == RouteState::DirectLocked && previous != RouteState::DirectLocked) ||
		(next == RouteState::GalaxyLocked && previous != RouteState::GalaxyLocked))
	{
		LogTerminalRoute(physicalPrimary, reason, now);
	}
}

} // namespace bf2direct
