using System.Windows;
using Fohjin.DDD.BankApplication.Wpf.ViewModels;

namespace Fohjin.DDD.BankApplication.Wpf.Views;

public partial class MonitoringWindow : Window
{
    public MonitoringWindow(MonitoringViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
