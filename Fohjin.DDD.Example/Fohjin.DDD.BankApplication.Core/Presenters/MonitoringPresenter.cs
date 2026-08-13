using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Common;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.BankApplication.Presenters;

public class MonitoringPresenter : Presenter<IMonitoringView>, IMonitoringPresenter
{
    private readonly IMonitoringView _monitoringView;

    public MonitoringPresenter(
        IMonitoringView monitoringView,
        MonitoringLoggerProvider monitoringLoggerProvider,
        EventStreamClient eventStreamClient,
        ILogger<MonitoringPresenter> logger
        ) : base(monitoringView)
    {
        _monitoringView = monitoringView;

        // Subscribe/connect immediately (not in Display()) so nothing logged/published before
        // the window is first shown is lost - the view itself buffers everything it's given.
        // docs/07-messaging-bus.md / docs/09-client-uis.md: this used to be bus.Events.Subscribe(...), the
        // same in-process IObservable<IDomainEvent> Fohjin.DDD.WebApi's SSE endpoint also
        // subscribes to server-side - now that WinForms doesn't host the CQRS core itself
        // anymore, it reaches the same events the same way Fohjin.DDD.WebUI's Monitoring.vue
        // does: as a client of GET /api/events. The log half (MonitoringLoggerProvider) needed
        // no change - it now captures HttpCallLoggingHandler's per-request log lines instead of
        // in-process bus/command-handler logging (see HttpCallLoggingHandler's own comment).
        monitoringLoggerProvider.LineLogged += _monitoringView.AppendLogLine;
        _ = ConsumeEventStreamAsync(eventStreamClient, logger);
    }

    private async Task ConsumeEventStreamAsync(EventStreamClient eventStreamClient, ILogger logger)
    {
        while (true)
        {
            try
            {
                await foreach (var domainEvent in eventStreamClient.StreamEventsAsync())
                {
                    _monitoringView.AppendEventLine(
                        $"{domainEvent.OccurredAt:HH:mm:ss.fff}  {domainEvent.EventType}  AggregateId={domainEvent.AggregateId}  Version={domainEvent.Version}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Event stream disconnected; retrying in 5s");
            }

            await Task.Delay(5000);
        }
    }

    public void Display() => _monitoringView.Show();
}
