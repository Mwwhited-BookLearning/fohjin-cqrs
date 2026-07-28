namespace Fohjin.DDD.BankApplication.Views;

public partial class MonitoringForm : ViewFormBase, IMonitoringView
{
    private const int MaxEntries = 1000;

    public MonitoringForm()
    {
        InitializeComponent();
    }

    public new void Show() => base.Show();

    public void AppendLogLine(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLogLine(line));
            return;
        }

        Append(_logsListBox, line);
    }

    public void AppendEventLine(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendEventLine(line));
            return;
        }

        Append(_eventsListBox, line);
    }

    private static void Append(ListBox listBox, string line)
    {
        listBox.Items.Add(line);
        if (listBox.Items.Count > MaxEntries)
            listBox.Items.RemoveAt(0);
        listBox.TopIndex = listBox.Items.Count - 1;
    }
}
