using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Fohjin.DDD.BankApplication.Wpf.ViewModels;
using Fohjin.DDD.DesktopClient;
using Microsoft.Extensions.DependencyInjection;

namespace Fohjin.DDD.BankApplication.Wpf.Services;

public class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public ObservableObject? CurrentViewModel { get; private set; }

    public NavigationService(IServiceProvider serviceProvider, DomainEventBus eventBus)
    {
        _serviceProvider = serviceProvider;
        // Dispatch to whichever ViewModel is CURRENTLY showing, rather than each ViewModel
        // subscribing/unsubscribing on navigation - avoids ever accumulating stale subscriptions
        // from screens the user has already navigated away from.
        eventBus.EventReceived += e => Application.Current.Dispatcher.Invoke(() =>
        {
            switch (CurrentViewModel)
            {
                case ClientSearchViewModel vm: vm.OnDomainEvent(e.EventType); break;
                case ClientDetailsViewModel vm: vm.OnDomainEvent(e.EventType, e.AggregateId); break;
                case AccountDetailsViewModel vm: vm.OnDomainEvent(e.EventType, e.AggregateId); break;
            }
        });
    }

    public void ShowClientSearch()
    {
        var viewModel = _serviceProvider.GetRequiredService<ClientSearchViewModel>();
        SetCurrent(viewModel);
        _ = viewModel.LoadCommand.ExecuteAsync(null);
    }

    public void ShowClientCreate() =>
        SetCurrent(_serviceProvider.GetRequiredService<ClientCreateViewModel>());

    public void ShowClientDetails(Guid clientId)
    {
        var viewModel = _serviceProvider.GetRequiredService<ClientDetailsViewModel>();
        viewModel.Initialize(clientId);
        SetCurrent(viewModel);
        _ = viewModel.LoadCommand.ExecuteAsync(null);
    }

    public void ShowAccountDetails(Guid accountId)
    {
        var viewModel = _serviceProvider.GetRequiredService<AccountDetailsViewModel>();
        viewModel.Initialize(accountId);
        SetCurrent(viewModel);
        _ = viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void SetCurrent(ObservableObject viewModel)
    {
        CurrentViewModel = viewModel;
        OnPropertyChanged(nameof(CurrentViewModel));
    }
}
