#include "direct_transport/direct_transport_server.h"
#include "Logger.h"

#include <Windows.h>

#include <cstdio>
#include <fstream>
#include <iterator>
#include <string>

namespace bf2direct
{

struct ServerTransportLoggingTestAccess
{
	static void PrepareAssociation(ServerTransport &transport, std::uint8_t slot, std::uint32_t generation,
								   std::uint32_t connectionId, PeerClassification classification, RouteState state,
								   std::uint64_t startMs) noexcept
	{
		auto &association = transport.associations_[slot];
		association = {};
		association.live = true;
		association.generation = generation;
		association.connectionId = connectionId;
		association.classification = classification;
		association.state = state;
#if defined(_DEBUG)
		association.startMs = startMs;
#else
		(void)startMs;
#endif
	}

	static void SetDirectActivity(ServerTransport &transport, std::uint8_t slot, std::uint64_t startMs,
								  const AssociationSupportCounters &support) noexcept
	{
#if defined(_DEBUG)
		auto &association = transport.associations_[slot];
		association.enteredDirect = true;
		association.directStartMs = startMs;
		association.support = support;
#else
		(void)transport;
		(void)slot;
		(void)startMs;
		(void)support;
#endif
	}

	static void SetMilestone(ServerTransport &transport, std::uint8_t slot, HandshakeMilestone milestone,
							 std::uint8_t peerProtocol = 0) noexcept
	{
#if defined(_DEBUG)
		auto &association = transport.associations_[slot];
		association.lastMilestone = milestone;
		association.peerProtocol = peerProtocol;
#else
		(void)transport;
		(void)slot;
		(void)milestone;
		(void)peerProtocol;
#endif
	}

	static void SetOfferAttempts(ServerTransport &transport, std::uint8_t slot, std::uint8_t attempts) noexcept
	{
#if defined(_DEBUG)
		transport.associations_[slot].offerAttempts = attempts;
#else
		(void)transport;
		(void)slot;
		(void)attempts;
#endif
	}

	static void LogStartup(ServerTransport &transport) noexcept
	{
		transport.policy_ = Policy::PreferDirect;
		transport.gamePort_ = 3658;
		transport.LogStartup();
	}

	static void LogRoute(ServerTransport &transport, std::uint8_t slot, RouteReason reason, std::uint64_t now,
						 RemovalAction removal = RemovalAction::None) noexcept
	{
		transport.LogTerminalRoute(slot, reason, now, removal);
	}

	static void LogEnd(ServerTransport &transport, std::uint8_t slot, LifecycleReason reason,
					   std::uint64_t now) noexcept
	{
		transport.LogAssociationEnd(slot, reason, now);
	}

#if defined(_DEBUG)
	static void LogSocketError(ServerTransport &transport, bool send, std::uint32_t error,
							   bool) noexcept
	{
		transport.LogSocketError(send ? ServerTransport::SupportSocketOperation::Send
									  : ServerTransport::SupportSocketOperation::Receive,
								 error);
	}
#endif

	static void RecordOverflow(ServerTransport &transport, std::uint32_t discarded) noexcept
	{
		transport.RecordOverflow(discarded);
	}

	static void RecordInvariant(ServerTransport &transport) noexcept
	{
		transport.RecordInvariant(ServerTransport::ServerInvariant::IllegalRouteTransition, 4, 0, 1, 2);
	}
};

} // namespace bf2direct

namespace
{

std::string ReadLog()
{
	std::ifstream stream("rconserver_log.txt", std::ios::binary);
	return {std::istreambuf_iterator<char>(stream), std::istreambuf_iterator<char>()};
}

std::size_t Occurrences(const std::string &text, const char *value)
{
	std::size_t count{};
	std::size_t offset{};
	while ((offset = text.find(value, offset)) != std::string::npos)
	{
		++count;
		offset += std::char_traits<char>::length(value);
	}
	return count;
}

} // namespace

int main()
{
	using namespace bf2direct;
	DeleteFileA("rconserver_log.txt");
	Logger.SetMinLevelFile(LogLevel_VERBOSE);

	ServerTransport startup;
	ServerTransportLoggingTestAccess::LogStartup(startup);

	ServerTransport direct;
	ServerTransportLoggingTestAccess::PrepareAssociation(direct, 4, 7, 0x8F26A1C3, PeerClassification::Patched,
														 RouteState::DirectLocked, 100);
	ServerTransportLoggingTestAccess::SetMilestone(direct, 4, HandshakeMilestone::Commit);
	ServerTransportLoggingTestAccess::LogRoute(direct, 4, RouteReason::Commit, 284);
	ServerTransportLoggingTestAccess::LogRoute(direct, 4, RouteReason::Commit, 300);

	AssociationSupportCounters support{};
	support.transmitDatagrams = 10;
	support.transmitBytes = 6000;
	support.receiveDatagrams = 9;
	support.receiveBytes = 5400;
	support.replayRejects = 1;
	ServerTransportLoggingTestAccess::SetDirectActivity(direct, 4, 150, support);
	ServerTransportLoggingTestAccess::LogEnd(direct, 4, LifecycleReason::Shutdown, 500);

	ServerTransport zeroData;
	ServerTransportLoggingTestAccess::PrepareAssociation(zeroData, 3, 11, 0x0BADF00D, PeerClassification::Patched,
														 RouteState::DirectLocked, 400);
	ServerTransportLoggingTestAccess::SetDirectActivity(zeroData, 3, 400, {});
	ServerTransportLoggingTestAccess::LogEnd(zeroData, 3, LifecycleReason::Shutdown, 450);

	ServerTransport vanilla;
	ServerTransportLoggingTestAccess::PrepareAssociation(vanilla, 5, 8, 0, PeerClassification::Vanilla,
														 RouteState::GalaxyLocked, 1000);
	ServerTransportLoggingTestAccess::LogRoute(vanilla, 5, RouteReason::CapabilityTimeout, 6000);
	ServerTransportLoggingTestAccess::LogEnd(vanilla, 5, LifecycleReason::NativeDisconnect, 7000);

	ServerTransport removed;
	ServerTransportLoggingTestAccess::PrepareAssociation(removed, 6, 9, 0, PeerClassification::Incompatible,
														 RouteState::Unclassified, 100);
	ServerTransportLoggingTestAccess::SetMilestone(removed, 6, HandshakeMilestone::Caps, 2);
	ServerTransportLoggingTestAccess::LogRoute(removed, 6, RouteReason::ProtocolIncompatibility, 150);
	ServerTransportLoggingTestAccess::LogRoute(removed, 6, RouteReason::ProtocolIncompatibility, 200,
											   RemovalAction::RequireDirectPatched);
	ServerTransportLoggingTestAccess::LogEnd(removed, 6, LifecycleReason::ServerRemoval, 210);

	ServerTransport submission;
	ServerTransportLoggingTestAccess::PrepareAssociation(submission, 7, 10, 0x01020304, PeerClassification::Patched,
														 RouteState::GalaxyLocked, 100);
	ServerTransportLoggingTestAccess::SetMilestone(submission, 7, HandshakeMilestone::Offer);
	ServerTransportLoggingTestAccess::SetOfferAttempts(submission, 7, 3);
	ServerTransportLoggingTestAccess::LogRoute(submission, 7, RouteReason::SubmissionTimeout, 5100);

#if defined(_DEBUG)
	ServerTransport socketErrors;
	for (int index{}; index < 4; ++index)
	{
		ServerTransportLoggingTestAccess::LogSocketError(socketErrors, true, 10054, true);
	}
	ServerTransportLoggingTestAccess::LogSocketError(socketErrors, false, 777, false);
	for (std::uint32_t error = 1; error <= 8; ++error)
	{
		ServerTransportLoggingTestAccess::LogSocketError(socketErrors, true, error, true);
	}
	ServerTransportLoggingTestAccess::LogSocketError(socketErrors, false, 999, true);
#endif

	ServerTransport overflow;
	ServerTransportLoggingTestAccess::RecordOverflow(overflow, 2);
	ServerTransportLoggingTestAccess::RecordOverflow(overflow, 3);
	ServerTransportLoggingTestAccess::RecordOverflow(overflow, 4);
	ServerTransportLoggingTestAccess::RecordOverflow(overflow, 5);

	ServerTransport endpoint;
	endpoint.ReportEndpointObservation(EndpointAddressClass::Public, true, true);
	endpoint.ReportEndpointObservation(EndpointAddressClass::Public, true, true);
	endpoint.ReportEndpointObservation(EndpointAddressClass::Private, false, false);

	ServerTransport invariant;
	ServerTransportLoggingTestAccess::RecordInvariant(invariant);

	const auto log = ReadLog();
#if defined(_DEBUG)
	if (Occurrences(log, "Direct transport started:") != 1 || Occurrences(log, "Direct route:") != 3 ||
		Occurrences(log, "Direct end:") != 3 || log.find("connection=0x8F26A1C3") == std::string::npos ||
		log.find("last-stage=COMMIT elapsed-ms=184") == std::string::npos ||
		log.find("direct-ms=350 tx=10/6000 rx=9/5400 replay-rejects=1") == std::string::npos ||
		log.find("connection=0x0BADF00D classification=Patched route=Direct reason=Shutdown direct-ms=50 "
				 "tx=0/0 rx=0/0") == std::string::npos ||
		log.find("peer-protocol=2") == std::string::npos ||
		log.find("failed-operation=OFFER attempts=3") == std::string::npos ||
		log.find("route=Removed reason=ServerRemoval") == std::string::npos)
	{
		return 1;
	}
#else
	if (log.find("Direct transport started:") != std::string::npos || log.find("Direct route:") != std::string::npos ||
		log.find("Direct end:") != std::string::npos)
	{
		return 1;
	}
#endif
	if (Occurrences(log, "Direct transport removed peer:") != 1 ||
		log.find("classification=Incompatible connection=none action=Removed "
				 "policy=RequireDirectPatched reason=ProtocolIncompatibility") == std::string::npos)
	{
		return 2;
	}
#if defined(_DEBUG)
	if (Occurrences(log, "Direct transport socket error:") != 10 || log.find("occurrence=3") != std::string::npos ||
		Occurrences(log, "socket-error table full") != 1 ||
		log.find("later error pairs are not logged individually") == std::string::npos)
	{
		return 3;
	}
	if (log.find("operation=receive error=777 occurrence=1") == std::string::npos) return 7;
#else
	if (log.find("Direct transport socket error:") != std::string::npos ||
		log.find("socket-error table full") != std::string::npos)
	{
		return 3;
	}
#endif
	if (Occurrences(log, "Direct transport receive overflow:") != 3 ||
		log.find("discarded=14 occurrences=4") == std::string::npos)
	{
		return 4;
	}
	if (Occurrences(log, "Direct endpoint changed:") != 2 ||
		log.find("address=public observed-port=usable") == std::string::npos ||
		log.find("address=private observed-port=unusable") == std::string::npos ||
		log.find("observed-port=unusable error=") != std::string::npos)
	{
		return 5;
	}
	if (log.find("invariant=illegal route transition occurrence=1 slot=4") == std::string::npos)
	{
		return 6;
	}
	DeleteFileA("rconserver_log.txt");
	return 0;
}
