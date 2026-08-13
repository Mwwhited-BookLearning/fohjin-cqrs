using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Fohjin.DDD.Diagnostics;

// Shared ActivitySource/Meter for the CQRS pipeline (Bus, MessageRouting, CommandHandlers,
// EventStore/EventStore.SqlServer). Lives here because Fohjin.DDD.Abstractions is the one
// project every pipeline project already reaches (directly or transitively), with zero
// ProjectReferences of its own to entangle. Fohjin.DDD.ServiceDefaults has no ProjectReferences
// by design (it's meant to be a portable, domain-agnostic Aspire "add this to any service"
// file) and can't reference this type directly - it registers this same name as a literal
// string instead (ConfigureOpenTelemetry's AddSource/AddMeter calls). Keep the name in sync by
// hand in both places if it ever changes.
public static class Telemetry
{
    public const string ServiceName = "Fohjin.DDD.Cqrs";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> CommandsHandled = Meter.CreateCounter<long>(
        "cqrs.commands.handled",
        unit: "{command}",
        description: "Commands executed through TransactionHandler, tagged by command type and outcome.");

    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "cqrs.command.duration",
        unit: "s",
        description: "Time to run a command handler and commit the unit of work.");

    public static readonly Counter<long> EventsPublished = Meter.CreateCounter<long>(
        "cqrs.events.published",
        unit: "{event}",
        description: "Domain events published onto the bus - one per event regardless of handler fan-out.");

    public static readonly Counter<long> EventsHandled = Meter.CreateCounter<long>(
        "cqrs.events.handled",
        unit: "{event}",
        description: "Event-handler invocations, tagged by event type, handler type, and outcome.");

    public static readonly Histogram<double> EventHandlerDuration = Meter.CreateHistogram<double>(
        "cqrs.event.handler.duration",
        unit: "s",
        description: "Time for a single event-handler invocation to complete.");
}
