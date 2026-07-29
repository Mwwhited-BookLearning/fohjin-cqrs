using System.Windows.Controls;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Wpf.ViewModels;

namespace Fohjin.DDD.BankApplication.Wpf.Views;

public partial class ClientDetailsView : UserControl
{
    public ClientDetailsView() => InitializeComponent();

    private void AccountItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ClientDetailsViewModel viewModel && sender is ListBoxItem { DataContext: AccountReport account })
            viewModel.OpenAccountCommand.Execute(account);
    }
}
