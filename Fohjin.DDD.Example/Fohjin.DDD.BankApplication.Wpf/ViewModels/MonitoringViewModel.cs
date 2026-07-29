using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fohjin.DDD.Common;
using Fohjin.DDD.DesktopClient;

namespace Fohjin.DDD.BankApplication.Wpf.ViewModels;

// WPF's equivalent of WinForms' MonitoringPresenter/MonitoringForm - a non-modal companion window
// shown alongside the main one at startup (App.xaml.cs), with the same two bounded lists (HTTP
// calls on one side, domain events on the other) capped at a fixed count so a long session
// doesn't grow memory unboundedly.
public partial class MonitoringViewModel : ObservableObject
{
    private const int MaxLines = 200;

    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<string> EventLines { get; } = [];

    public MonitoringViewModel(MonitoringLoggerProvider loggerProvider, DomainEventBus eventBus)
    {
        loggerProvider.LineLogged += line => Application.Current.Dispatcher.Invoke(() => AppendBounded(LogLines, line));
        eventBus.EventReceived += e => Application.Current.Dispatcher.Invoke(() => AppendBounded(EventLines,
            $"{e.OccurredAt:HH:mm:ss.fff}  {e.EventType}  AggregateId={e.AggregateId}  Version={e.Version}"));
    }

    private static void AppendBounded(ObservableCollection<string> lines, string line)
    {
        lines.Add(line);
        while (lines.Count > MaxLines) lines.RemoveAt(0);
    }

    [RelayCommand]
    private void ClearLogs() => LogLines.Clear();

    [RelayCommand]
    private void ClearEvents() => EventLines.Clear();
}
