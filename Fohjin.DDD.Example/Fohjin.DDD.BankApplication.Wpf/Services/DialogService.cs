using System.Windows;

namespace Fohjin.DDD.BankApplication.Wpf.Services;

public class DialogService : IDialogService
{
    public void ShowError(string message) =>
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
