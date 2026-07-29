using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Wpf.Services;

namespace Fohjin.DDD.BankApplication.Wpf.ViewModels;

public partial class ClientSearchViewModel : ViewModelBase
{
    private readonly FohjinApiClient _apiClient;
    private readonly INavigationService _navigation;

    // Kept as ONE long-lived instance rather than reassigned on every load - CollectionViewSource
    // binds to this specific collection instance, so replacing it wholesale (rather than
    // Clear()+Add()ing into it) would silently detach the live-filtering view below.
    public ObservableCollection<ClientReport> Clients { get; } = [];
    public ICollectionView ClientsView { get; }

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private bool isLoading = true;

    public ClientSearchViewModel(FohjinApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;
        ClientsView = CollectionViewSource.GetDefaultView(Clients);
        ClientsView.Filter = FilterClient;
    }

    partial void OnSearchTermChanged(string value) => ClientsView.Refresh();

    // Structure layer equivalent of Fohjin.DDD.WebUI/src/views/ClientSearch.config.ts's
    // matchesSearchTerm - same rule (case-insensitive substring of the client's name),
    // client-side only since neither this nor any other client calls the /odata/Clients
    // $filter endpoint today (docs/08-reporting-read-models.md).
    private bool FilterClient(object item)
    {
        if (item is not ClientReport client) return false;
        var term = SearchTerm.Trim();
        if (term.Length == 0) return true;
        return client.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafelyAsync(async () =>
        {
            IsLoading = true;
            var result = await _apiClient.GetClientsAsync();
            Clients.Clear();
            foreach (var client in result) Clients.Add(client);
            IsLoading = false;
        });
    }

    [RelayCommand]
    private void OpenClient(ClientReport? client)
    {
        if (client?.Id is { } id) _navigation.ShowClientDetails(id);
    }

    [RelayCommand]
    private void CreateClient() => _navigation.ShowClientCreate();

    // Event-driven refresh instead of a poll - same rule Vue's refreshRules.ts encodes
    // (shouldRefreshClientSearch: only ClientCreatedEvent matters here, since its AggregateId is
    // the new client's own id, not something already known before it happens).
    public void OnDomainEvent(string eventType)
    {
        if (eventType != "ClientCreatedEvent") return;
        _ = LoadAsyncWithReconciliation();
    }

    // See Fohjin.DDD.WebUI/src/composables/useClientSearch.ts's watchLiveEvents for why this
    // reloads twice - the SSE notification and the reporting-store's own DB write are
    // independent subscriptions on the same event with no ordering guarantee, found live while
    // verifying the Vue equivalent of this same screen.
    private async Task LoadAsyncWithReconciliation()
    {
        await LoadCommand.ExecuteAsync(null);
        await Task.Delay(750);
        await LoadCommand.ExecuteAsync(null);
    }
}
