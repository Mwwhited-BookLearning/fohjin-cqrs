using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Bus;
using Fohjin.DDD.Common;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.BankApplication.Presenters;

public class MonitoringPresenter : Presenter<IMonitoringView>, IMonitoringPresenter
{
    private readonly IMonitoringView _monitoringView;

    public MonitoringPresenter(
        IMonitoringView monitoringView,
        MonitoringLoggerProvider monitoringLoggerProvider,
        IBus bus
        ) : base(monitoringView)
    {
        _monitoringView = monitoringView;

        // Subscribe immediately (not in Display()) so nothing logged/published before the
        // window is first shown is lost - the view itself buffers everything it's given.
        monitoringLoggerProvider.LineLogged += _monitoringView.AppendLogLine;
        bus.Events.Subscribe(OnDomainEvent);
    }

    private void OnDomainEvent(IDomainEvent domainEvent) =>
        _monitoringView.AppendEventLine(
            $"{DateTime.Now:HH:mm:ss.fff}  {domainEvent.GetType().Name}  AggregateId={domainEvent.AggregateId}  Version={domainEvent.Version}");

    public void Display() => _monitoringView.Show();
}
