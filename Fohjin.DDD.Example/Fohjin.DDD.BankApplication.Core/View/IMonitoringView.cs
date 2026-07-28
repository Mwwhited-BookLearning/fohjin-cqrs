namespace Fohjin.DDD.BankApplication.Views;

public interface IMonitoringView : IView
{
    void Show();
    void AppendLogLine(string line);
    void AppendEventLine(string line);
}
