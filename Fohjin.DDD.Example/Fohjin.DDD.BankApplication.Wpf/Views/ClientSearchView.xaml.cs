using System.Windows.Controls;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Wpf.ViewModels;

namespace Fohjin.DDD.BankApplication.Wpf.Views;

public partial class ClientSearchView : UserControl
{
    public ClientSearchView() => InitializeComponent();

    private void ClientItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is ClientSearchViewModel viewModel && sender is ListBoxItem { DataContext: ClientReport client })
            viewModel.OpenClientCommand.Execute(client);
    }
}
