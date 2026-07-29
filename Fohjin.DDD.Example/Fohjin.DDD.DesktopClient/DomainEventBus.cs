using Fohjin.DDD.BankApplication.Presenters;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.DesktopClient;

// A shared, app-wide domain-event stream for a desktop client, mirroring
// Fohjin.DDD.WebUI/src/events/eventBus.ts's shape (docs/07-messaging-bus.md): one connection,
// N independent subscribers, instead of each screen opening its own. WinForms' MonitoringPresenter
// still consumes EventStreamClient directly (it's the only WinForms screen that's event-driven at
// all - see docs/09-client-uis.md's "what's genuinely different" note); this is what
// Fohjin.DDD.BankApplication.Wpf's ViewModels subscribe to instead, since every WPF screen reacts
// to live events, not just Monitoring.
public sealed class DomainEventBus(EventStreamClient client, ILogger<DomainEventBus> logger)
{
    public event Action<EventEnvelope>? EventReceived;

    private bool _started;

    public void Start()
    {
        if (_started) return;
        _started = true;
        _ = RunForeverAsync();
    }

    private async Task RunForeverAsync()
    {
        while (true)
        {
            try
            {
                await foreach (var domainEvent in client.StreamEventsAsync())
                    EventReceived?.Invoke(domainEvent);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Event stream disconnected; retrying in 5s");
            }

            await Task.Delay(5000);
        }
    }
}
