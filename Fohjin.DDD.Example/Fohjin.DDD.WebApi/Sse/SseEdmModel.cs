using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Fohjin.DDD.WebApi.Sse;

public static class SseEdmModel
{
    // Filterable fields only: Id, AggregateId, Version, EventType, OccurredAt. Payload (the raw
    // IDomainEvent) is excluded - it's an interface, not something the OData model builder can
    // reason about, and it isn't meant to be filtered on anyway. Reuses the exact same
    // ODataQueryOptions/EDM-model machinery /odata/Clients uses (docs/08-reporting-read-models.md)
    // rather than a hand-rolled filter grammar, just applied per-event instead of
    // per-request-against-SQL.
    public static IEdmModel Build()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<EventEnvelope>("Events").EntityType.Ignore(x => x.Payload);
        return builder.GetEdmModel();
    }
}
