using Fohjin.DDD.WebApi.Sse;
using Saunter.Attributes;

namespace Fohjin.DDD.WebApi.AsyncApi;

// Documents the /api/events SSE stream as AsyncAPI (Saunter) - OpenAPI has no vocabulary for a
// long-lived server-push stream, AsyncAPI's channel/message model does
// (docs/supporting/asyncapi-saunter.md). One channel, one subscribe operation, message type
// EventEnvelope - that's the actual wire contract every connected client receives (Saunter only
// allows one subscribe operation per channel key; modeling this as 17 operations, one per
// concrete domain event type, hit that limit at startup, and would have described a shape
// that was never really on the wire anyway - clients always get an EventEnvelope, whose
// Payload is the polymorphic IDomainEvent, not a bare domain event). Deliberately describes
// the content contract only - the $filter query syntax clients use to narrow the stream is a
// query-time concern, already documented on the OpenAPI side (the equivalent-GET-route trick
// from Phase 3), not something AsyncAPI models.
[AsyncApi]
public class DomainEventsAsyncApi
{
    [Channel("api/events")]
    [SubscribeOperation(typeof(EventEnvelope), "domain-events")]
    public void Events() { }
}
