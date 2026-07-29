using System.Windows;
using Fohjin.DDD.BankApplication.Wpf.Services;

namespace Fohjin.DDD.BankApplication.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(INavigationService navigationService)
    {
        InitializeComponent();
        DataContext = navigationService;
    }
}
