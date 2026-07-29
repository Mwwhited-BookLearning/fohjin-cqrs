using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Wpf.Services;

namespace Fohjin.DDD.BankApplication.Wpf.ViewModels;

public partial class ClientCreateViewModel : ViewModelBase
{
    private readonly FohjinApiClient _apiClient;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private string clientName = string.Empty;
    [ObservableProperty]
    private string street = string.Empty;
    [ObservableProperty]
    private string streetNumber = string.Empty;
    [ObservableProperty]
    private string postalCode = string.Empty;
    [ObservableProperty]
    private string city = string.Empty;
    [ObservableProperty]
    private string phoneNumber = string.Empty;
    [ObservableProperty]
    private bool submitting;

    public ClientCreateViewModel(FohjinApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        await RunSafelyAsync(async () =>
        {
            Submitting = true;
            await _apiClient.CreateClientAsync(new CreateClientRequest
            {
                ClientName = ClientName,
                Street = Street,
                StreetNumber = StreetNumber,
                PostalCode = PostalCode,
                City = City,
                PhoneNumber = PhoneNumber,
            });
            Submitting = false;
            // CreateClientCommand.Id isn't the persisted client's id (Fohjin.DDD.WebApi/Program.cs)
            // and DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so
            // there's no id to navigate straight to - go back to the search list, which will
            // show the new client once the command has actually been handled.
            _navigation.ShowClientSearch();
        });
    }

    [RelayCommand]
    private void Cancel() => _navigation.ShowClientSearch();
}
