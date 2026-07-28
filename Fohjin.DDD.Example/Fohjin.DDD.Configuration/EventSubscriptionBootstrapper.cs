using Fohjin.DDD.Bus;
using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Reflection;

namespace Fohjin.DDD.Configuration
{
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

                SubscribeMethod.MakeGenericMethod(eventType).Invoke(null, new object[] { bus.Events, handler, log });
            }

            return serviceProvider;
        }

        private static IDisposable Subscribe<TEvent>(IObservable<IDomainEvent> events, IEventHandler handler, ILogger log)
            where TEvent : class, IDomainEvent =>
            events.Select(e => (object)e).OfType<TEvent>().Subscribe(async e =>
            {
                try
                {
                    await handler.ExecuteAsync(e);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Event handler {Handler} failed for {Event}", handler.GetType().Name, e);
                }
            });
    }
}
