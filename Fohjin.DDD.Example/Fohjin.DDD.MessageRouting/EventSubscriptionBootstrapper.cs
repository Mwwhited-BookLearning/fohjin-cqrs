using Fohjin.DDD.Bus;
using Fohjin.DDD.Diagnostics;
using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reflection;

namespace Fohjin.DDD.MessageRouting;

// Replaces the old reflection-per-message EventHandlerHelper: each registered IEventHandler<T>
// is wired, once at startup, into an Rx query chain over the bus's event stream, instead of
// being looked up by type on every single published event.
public static class EventSubscriptionBootstrapper
{
    private static readonly MethodInfo SubscribeMethod = typeof(EventSubscriptionBootstrapper)
        .GetMethod(nameof(Subscribe), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IServiceProvider SubscribeEventHandlers(this IServiceProvider serviceProvider)
    {
        var bus = serviceProvider.GetRequiredService<IBus>();
        var log = serviceProvider.GetRequiredService<ILogger<IEventHandler>>();

        foreach (var handler in serviceProvider.GetServices<IEventHandler>())
        {
            var eventType = handler.GetType().GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .GetGenericArguments()[0];

            SubscribeMethod.MakeGenericMethod(eventType).Invoke(null, [bus.Events, handler, log]);
        }

        return serviceProvider;
    }

    private static IDisposable Subscribe<TEvent>(IObservable<IDomainEvent> events, IEventHandler handler, ILogger log)
        where TEvent : class, IDomainEvent
    {
        var eventType = typeof(TEvent).Name;
        var handlerType = handler.GetType().Name;

        return events.Select(e => (object)e).OfType<TEvent>().Subscribe(async e =>
        {
            using var activity = Telemetry.ActivitySource.StartActivity($"event.handle {eventType}");
            activity?.SetTag("cqrs.event.type", eventType);
            activity?.SetTag("cqrs.event.handler.type", handlerType);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await handler.ExecuteAsync(e);
                RecordOutcome(eventType, handlerType, "success", stopwatch.Elapsed, activity);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Event handler {Handler} failed for {Event}", handlerType, e);
                RecordOutcome(eventType, handlerType, "failure", stopwatch.Elapsed, activity);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
            }
        });
    }

    private static void RecordOutcome(string eventType, string handlerType, string outcome, TimeSpan elapsed, Activity? activity)
    {
        var tags = new TagList
        {
            { "cqrs.event.type", eventType },
            { "cqrs.event.handler.type", handlerType },
            { "cqrs.outcome", outcome },
        };
        Telemetry.EventsHandled.Add(1, tags);
        Telemetry.EventHandlerDuration.Record(elapsed.TotalSeconds, tags);
        activity?.SetTag("cqrs.outcome", outcome);
    }
}
