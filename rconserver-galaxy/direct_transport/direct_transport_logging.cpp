#include "direct_transport/direct_transport_server.h"

#include "direct_transport/direct_transport_policy.h"
#include "Logger.h"

#include <algorithm>
#include <cstdarg>
#include <cstdio>

namespace bf2direct
{
namespace
{

constexpr std::uint32_t kEndpointStateInitialized = 1u << 0;
constexpr std::uint32_t kEndpointStateUsable = 1u << 1;
constexpr std::uint32_t kEndpointStatePortUsable = 1u << 2;
constexpr std::uint32_t kEndpointStateClassShift = 3;

void Append(char *destination, std::size_t capacity, std::size_t &length, const char *format, ...) noexcept
{
	if (!destination || !capacity || length >= capacity - 1) return;
	va_list arguments;
	va_start(arguments, format);
	const auto written = std::vsnprintf(destination + length, capacity - length, format, arguments);
	va_end(arguments);
	if (written <= 0) return;
	length += (std::min)(static_cast<std::size_t>(written), capacity - length - 1);
}

bool Any(const AssociationSupportCounters &counters) noexcept
{
	return counters.transmitDatagrams || counters.transmitBytes || counters.receiveDatagrams || counters.receiveBytes ||
		   counters.sendErrors || counters.endpointRejects || counters.authenticationRejects ||
		   counters.replayRejects || counters.lifecycleRejects;
}

bool IsPowerOfTwo(std::uint64_t value) noexcept
{
	return value && !(value & (value - 1));
}

const char *RouteName(RouteState state, RemovalAction removal) noexcept
{
	if (removal != RemovalAction::None) return "Removed";
	return state == RouteState::DirectLocked ? "Direct" : "Galaxy";
}

void FormatConnection(char (&destination)[16], std::uint32_t connectionId) noexcept
{
	if (connectionId)
		_snprintf_s(destination, sizeof(destination), _TRUNCATE, "0x%08X", connectionId);
	else
		strcpy_s(destination, "none");
}

} // namespace

void ServerTransport::CompleteMilestone(Association &association, HandshakeMilestone milestone) noexcept
{
#if defined(_DEBUG)
	if (static_cast<std::uint8_t>(milestone) > static_cast<std::uint8_t>(association.lastMilestone))
	{
		association.lastMilestone = milestone;
	}
#else
	(void)association;
	(void)milestone;
#endif
}

void ServerTransport::LogStartup() const noexcept
{
#if defined(_DEBUG)
	const auto endpointState = endpointSupportState_.load(std::memory_order_acquire);
	const char *endpoint = "pending";
	if (endpointState & kEndpointStateInitialized)
	{
		endpoint = endpointState & kEndpointStateUsable ? "usable" : "unavailable";
	}
	Logger.log(LogLevel_INFO, "Direct transport started: policy=%s protocol=%u gameport=%u endpoint=%s",
			   PolicyName(policy_), kProtocolVersion, gamePort_, endpoint);
#endif
}

void ServerTransport::LogTerminalRoute(std::uint8_t physicalPrimary, RouteReason reason, std::uint64_t now,
									   RemovalAction removal) noexcept
{
	if (physicalPrimary >= associations_.size()) return;
	auto &association = associations_[physicalPrimary];
	if (!association.live) return;
	if (removal != RemovalAction::None)
	{
		if (association.removalAction != RemovalAction::None) return;
		association.removalAction = removal;
		char connection[16]{};
		FormatConnection(connection, association.connectionId);
		Logger.log(LogLevel_WARNING,
				   "Direct transport removed peer: slot=%u classification=%s connection=%s "
				   "action=Removed policy=%s reason=%s",
				   physicalPrimary, PeerClassificationName(association.classification), connection,
				   RemovalActionName(removal), RouteReasonName(reason));
		return;
	}

#if defined(_DEBUG)
	if (association.terminalRouteLogged) return;
	association.terminalRouteLogged = true;
	const auto ordinaryVanilla = association.classification == PeerClassification::Vanilla &&
								 association.connectionId == 0 && reason == RouteReason::CapabilityTimeout &&
								 !Any(association.support);
	if (ordinaryVanilla) return;
	char connection[16]{};
	FormatConnection(connection, association.connectionId);
	const auto elapsed = now >= association.startMs ? now - association.startMs : 0;
	if (reason == RouteReason::ProtocolIncompatibility)
	{
		Logger.log(LogLevel_VERBOSE,
				   "Direct route: slot=%u generation=%u connection=%s classification=%s route=%s "
				   "reason=%s last-stage=%s elapsed-ms=%llu peer-protocol=%u",
				   physicalPrimary, association.generation, connection,
				   PeerClassificationName(association.classification), RouteName(association.state, removal),
				   RouteReasonName(reason), HandshakeMilestoneName(association.lastMilestone),
				   static_cast<unsigned long long>(elapsed), association.peerProtocol);
		return;
	}
	if (reason == RouteReason::SubmissionTimeout)
	{
		const auto commit = association.lastMilestone >= HandshakeMilestone::Ready;
		Logger.log(LogLevel_VERBOSE,
				   "Direct route: slot=%u generation=%u connection=%s classification=%s route=%s "
				   "reason=%s last-stage=%s elapsed-ms=%llu failed-operation=%s attempts=%u",
				   physicalPrimary, association.generation, connection,
				   PeerClassificationName(association.classification), RouteName(association.state, removal),
				   RouteReasonName(reason), HandshakeMilestoneName(association.lastMilestone),
				   static_cast<unsigned long long>(elapsed), commit ? "COMMIT" : "OFFER",
				   commit ? association.commitAttempts : association.offerAttempts);
		return;
	}
	Logger.log(LogLevel_VERBOSE,
			   "Direct route: slot=%u generation=%u connection=%s classification=%s route=%s reason=%s "
			   "last-stage=%s elapsed-ms=%llu",
			   physicalPrimary, association.generation, connection, PeerClassificationName(association.classification),
			   RouteName(association.state, removal), RouteReasonName(reason),
			   HandshakeMilestoneName(association.lastMilestone), static_cast<unsigned long long>(elapsed));
#else
	(void)reason;
	(void)now;
#endif
}

void ServerTransport::LogAssociationEnd(std::uint8_t physicalPrimary, LifecycleReason reason,
										std::uint64_t now) noexcept
{
#if defined(_DEBUG)
	if (physicalPrimary >= associations_.size()) return;
	const auto &association = associations_[physicalPrimary];
	const auto relevant = association.classification == PeerClassification::Patched ||
						  association.classification == PeerClassification::Incompatible || association.connectionId ||
						  association.enteredDirect || association.removalAction != RemovalAction::None ||
						  Any(association.support);
	if (!association.live || !relevant) return;

	char connection[16]{};
	FormatConnection(connection, association.connectionId);
	char optional[640]{};
	std::size_t length{};
	if (association.enteredDirect)
	{
		Append(optional, sizeof(optional), length, " direct-ms=%llu tx=%llu/%llu rx=%llu/%llu",
			   static_cast<unsigned long long>(now >= association.directStartMs ? now - association.directStartMs : 0),
			   static_cast<unsigned long long>(association.support.transmitDatagrams),
			   static_cast<unsigned long long>(association.support.transmitBytes),
			   static_cast<unsigned long long>(association.support.receiveDatagrams),
			   static_cast<unsigned long long>(association.support.receiveBytes));
	}
	if (association.support.sendErrors)
	{
		Append(optional, sizeof(optional), length, " send-errors=%llu",
			   static_cast<unsigned long long>(association.support.sendErrors));
	}
	if (association.support.endpointRejects)
	{
		Append(optional, sizeof(optional), length, " endpoint-rejects=%llu",
			   static_cast<unsigned long long>(association.support.endpointRejects));
	}
	if (association.support.authenticationRejects)
	{
		Append(optional, sizeof(optional), length, " authentication-rejects=%llu",
			   static_cast<unsigned long long>(association.support.authenticationRejects));
	}
	if (association.support.replayRejects)
	{
		Append(optional, sizeof(optional), length, " replay-rejects=%llu",
			   static_cast<unsigned long long>(association.support.replayRejects));
	}
	if (association.support.lifecycleRejects)
	{
		Append(optional, sizeof(optional), length, " lifecycle-rejects=%llu",
			   static_cast<unsigned long long>(association.support.lifecycleRejects));
	}
	Logger.log(LogLevel_VERBOSE,
			   "Direct end: slot=%u generation=%u connection=%s classification=%s route=%s reason=%s%s",
			   physicalPrimary, association.generation, connection, PeerClassificationName(association.classification),
			   RouteName(association.state, association.removalAction), LifecycleReasonName(reason), optional);
#else
	(void)physicalPrimary;
	(void)reason;
	(void)now;
#endif
}

#if defined(_DEBUG)
void ServerTransport::LogSocketError(SupportSocketOperation operation, std::uint32_t error) noexcept
{
	SocketErrorEntry *entry{};
	for (auto &candidate : socketErrors_)
	{
		if (candidate.used && candidate.operation == operation && candidate.error == error)
		{
			entry = &candidate;
			break;
		}
		if (!candidate.used && !entry) entry = &candidate;
	}
	if (!entry || (entry->used && (entry->operation != operation || entry->error != error)))
	{
		if (!socketErrorTableSaturated_)
		{
			socketErrorTableSaturated_ = true;
			Logger.log(LogLevel_WARNING,
					   "Direct transport socket-error table full; later error pairs are not logged individually");
		}
		return;
	}
	if (!entry->used)
	{
		entry->used = true;
		entry->operation = operation;
		entry->error = error;
	}
	const auto occurrence = ++entry->occurrences;
	if (!IsPowerOfTwo(occurrence)) return;
	Logger.log(LogLevel_WARNING, "Direct transport socket error: operation=%s error=%u occurrence=%llu",
			   operation == SupportSocketOperation::Send ? "send" : "receive", error,
			   static_cast<unsigned long long>(occurrence));
}
#endif

void ServerTransport::RecordOverflow(std::uint32_t discarded) noexcept
{
	if (!discarded) return;
	overflowDiscards_ += discarded;
	const auto occurrence = ++overflowOccurrences_;
	if (!IsPowerOfTwo(occurrence)) return;
	Logger.log(LogLevel_WARNING, "Direct transport receive overflow: discarded=%llu occurrences=%llu",
			   static_cast<unsigned long long>(overflowDiscards_), static_cast<unsigned long long>(occurrence));
}

void ServerTransport::RecordInvariant(ServerInvariant invariant, std::uint8_t physicalPrimary, std::uint32_t error,
									  std::uint32_t value0, std::uint32_t value1) noexcept
{
	const auto index = static_cast<std::size_t>(invariant);
	if (index >= invariantCounts_.size()) return;
	const auto occurrence = ++invariantCounts_[index];
	if (IsPowerOfTwo(occurrence))
	{
		LogInvariant(invariant, occurrence, physicalPrimary, error, value0, value1);
	}
}

void ServerTransport::LogInvariant(ServerInvariant invariant, std::uint64_t occurrence, std::uint8_t physicalPrimary,
								   std::uint32_t error, std::uint32_t value0, std::uint32_t value1) noexcept
{
	const char *name{};
	switch (invariant)
	{
	case ServerInvariant::StaleGenerationOutput:
		name = "stale-generation output";
		break;
	case ServerInvariant::GroupDepthUnderflow:
		name = "group-depth underflow";
		break;
	case ServerInvariant::GroupDepthOverflow:
		name = "group-depth overflow";
		break;
	case ServerInvariant::ThreadViolation:
		name = "thread violation";
		break;
	case ServerInvariant::IllegalRouteTransition:
		name = "illegal route transition";
		break;
	case ServerInvariant::PostDirectGalaxySubmission:
		name = "post-Direct Galaxy submission";
		break;
	case ServerInvariant::IncompleteRollback:
		name = "incomplete rollback";
		break;
	case ServerInvariant::IncompleteShutdown:
		name = "incomplete shutdown";
		break;
	default:
		return;
	}
	Logger.log(LogLevel_ERROR,
			   "Direct transport invariant failed: invariant=%s occurrence=%llu slot=%u error=%u value0=%u value1=%u",
			   name, static_cast<unsigned long long>(occurrence), physicalPrimary, error, value0, value1);
}

void ServerTransport::ReportEndpointObservation(EndpointAddressClass addressClass, bool portUsable,
												bool usable) noexcept
{
	const auto state = kEndpointStateInitialized | (usable ? kEndpointStateUsable : 0u) |
					   (portUsable ? kEndpointStatePortUsable : 0u) |
					   (static_cast<std::uint32_t>(addressClass) << kEndpointStateClassShift);
	if (endpointSupportState_.load(std::memory_order_acquire) == state) return;
	const auto previous = endpointSupportState_.exchange(state, std::memory_order_acq_rel);
	if (previous == state) return;
	Logger.log(usable ? LogLevel_INFO : LogLevel_WARNING, "Direct endpoint changed: address=%s observed-port=%s",
			   EndpointAddressClassName(addressClass), portUsable ? "usable" : "unusable");
}

void ServerTransport::ReportEndpointObserverFailure(const char *reason, std::uint32_t error) noexcept
{
	if (error)
	{
		Logger.log(LogLevel_WARNING, "Direct transport public endpoint unavailable: reason=%s error=%u",
				   reason ? reason : "unknown", error);
	}
	else
	{
		Logger.log(LogLevel_WARNING, "Direct transport public endpoint unavailable: reason=%s",
				   reason ? reason : "unknown");
	}
}

} // namespace bf2direct
