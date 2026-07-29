using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Fohjin.DDD.BankApplication.Presenters;

// Hand-rolled SSE consumer for GET api/events - the generated FohjinApiClient.StreamEventsAsync()
// can't be used for this (NSwag has no real streaming-response support: it does a one-shot
// SendAsync and discards the body without ever reading it, confirmed by reading the generated
// method in Fohjin.DDD.ApiClient/obj/openapiClient.cs). System.Net.ServerSentEvents.SseParser<T>
// (same BCL namespace Fohjin.DDD.WebApi/Program.cs uses server-side via Results.ServerSentEvents)
// parses the wire format directly instead of hand-parsing "event:"/"data:" lines the way
// Fohjin.DDD.WebUI's Monitoring.vue has to (no equivalent parser exists on the TypeScript side).
public class EventStreamClient(HttpClient httpClient)
{
    // Results.ServerSentEvents (Fohjin.DDD.WebApi/Program.cs) serializes each item's data with
    // ASP.NET Core's default JSON options, which is JsonSerializerDefaults.Web (camelCase,
    // case-insensitive matching) - not JsonSerializer.Deserialize's own default (case-sensitive,
    // exact-name PascalCase). Deserializing without JsonSerializerOptions.Web here doesn't throw;
    // it silently binds nothing and returns an all-default EventEnvelope (EventType="",
    // AggregateId=Guid.Empty, OccurredAt=default) for every single event, real ones included -
    // found live via a WPF client-driven test where a domain event arrived (passed the wire-level
    // event-type filter below) but every ViewModel's OnDomainEvent(eventType, ...) check against
    // eventType silently never matched, so no event-driven screen ever reloaded.
    private static readonly JsonSerializerOptions Options = JsonSerializerOptions.Web;

    public async IAsyncEnumerable<EventEnvelope> StreamEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = await httpClient.GetStreamAsync("api/events", cancellationToken);
        var parser = SseParser.Create(stream, (eventType, data) => JsonSerializer.Deserialize<EventEnvelope>(data, Options));

        await foreach (var item in parser.EnumerateAsync(cancellationToken))
        {
            // Fohjin.DDD.WebApi/Sse/EventEnvelope.cs's EventEnvelope.Connected - sent the instant
            // a subscriber attaches purely to force Results.ServerSentEvents to flush the response
            // right away (it otherwise holds headers back until the first real item), not a real
            // domain event.
            if (item.Data is not null && item.EventType != "StreamConnected")
                yield return item.Data;
        }
    }
}

public record EventEnvelope(Guid Id, Guid AggregateId, int Version, string EventType, DateTimeOffset OccurredAt);
