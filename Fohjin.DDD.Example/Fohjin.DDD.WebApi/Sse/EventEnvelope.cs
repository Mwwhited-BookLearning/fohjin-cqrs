using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.WebApi.Sse;

// Wraps a raw IDomainEvent with the fields the /api/events SSE stream lets clients filter on.
// EventType/AggregateId/Version/Id come straight off the event (docs/11-migration-plan.md
// Phase 4 notes IDomainEvent has no built-in event-type or timestamp field); OccurredAt is
// synthesized here, at the moment the envelope is built for a subscriber, rather than read
// off the event or the event store - there's no "when this actually happened" timestamp
// anywhere in the pipeline today (aggregate, event store, or bus), so this is "observed by
// this stream at", not the event's true occurrence time.
public record EventEnvelope(Guid Id, Guid AggregateId, int Version, string EventType, DateTimeOffset OccurredAt, IDomainEvent Payload)
{
    public static EventEnvelope From(IDomainEvent domainEvent) =>
        new(domainEvent.Id, domainEvent.AggregateId, domainEvent.Version, domainEvent.GetType().Name, DateTimeOffset.UtcNow, domainEvent);
}
