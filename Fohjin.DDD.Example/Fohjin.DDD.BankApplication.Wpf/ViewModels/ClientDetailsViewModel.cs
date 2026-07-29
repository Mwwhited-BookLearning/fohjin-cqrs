using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Wpf.Services;

namespace Fohjin.DDD.BankApplication.Wpf.ViewModels;

public partial class ClientDetailsViewModel : ViewModelBase
{
    private readonly FohjinApiClient _apiClient;
    private readonly INavigationService _navigation;
    private Guid _clientId;

    [ObservableProperty]
    private bool isLoading = true;

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
    private string newAccountName = string.Empty;

    [ObservableProperty]
    private bool savingName;
    [ObservableProperty]
    private bool savingAddress;
    [ObservableProperty]
    private bool savingPhoneNumber;
    [ObservableProperty]
    private bool openingAccount;
    [ObservableProperty]
    private bool assigningBankCard;

    [ObservableProperty]
    private AccountReport? selectedAccount;
    [ObservableProperty]
    private BankCardReport? selectedBankCard;
    [ObservableProperty]
    private AccountReport? selectedNewBankCardAccount;

    public ObservableCollection<AccountReport> Accounts { get; } = [];
    public ObservableCollection<ClosedAccountReport> ClosedAccounts { get; } = [];
    public ObservableCollection<BankCardReport> BankCards { get; } = [];

    public ClientDetailsViewModel(FohjinApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;
    }

    public void Initialize(Guid clientId) => _clientId = clientId;

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafelyAsync(async () =>
        {
            IsLoading = true;
            var details = await _apiClient.GetClientDetailsByIdAsync(_clientId);

            ClientName = details.ClientName ?? "";
            Street = details.Street ?? "";
            StreetNumber = details.StreetNumber ?? "";
            PostalCode = details.PostalCode ?? "";
            City = details.City ?? "";
            PhoneNumber = details.PhoneNumber ?? "";

            Accounts.Clear();
            foreach (var a in details.Accounts ?? []) Accounts.Add(a);
            ClosedAccounts.Clear();
            foreach (var a in details.ClosedAccounts ?? []) ClosedAccounts.Add(a);
            BankCards.Clear();
            foreach (var c in details.BankCards ?? []) BankCards.Add(c);

            IsLoading = false;
        });
    }

    [RelayCommand]
    private async Task SaveNameAsync()
    {
        await RunSafelyAsync(async () =>
        {
            SavingName = true;
            await _apiClient.ChangeClientNameAsync(_clientId, new ChangeClientNameRequest { ClientName = ClientName });
            SavingName = false;
        });
    }

    [RelayCommand]
    private async Task SaveAddressAsync()
    {
        await RunSafelyAsync(async () =>
        {
            SavingAddress = true;
            await _apiClient.ChangeClientAddressAsync(_clientId, new ClientIsMovingRequest
            {
                Street = Street,
                StreetNumber = StreetNumber,
                PostalCode = PostalCode,
                City = City,
            });
            SavingAddress = false;
        });
    }

    [RelayCommand]
    private async Task SavePhoneNumberAsync()
    {
        await RunSafelyAsync(async () =>
        {
            SavingPhoneNumber = true;
            await _apiClient.ChangeClientPhoneNumberAsync(_clientId, new ChangeClientPhoneNumberRequest { PhoneNumber = PhoneNumber });
            SavingPhoneNumber = false;
        });
    }

    [RelayCommand]
    private async Task OpenNewAccountAsync()
    {
        await RunSafelyAsync(async () =>
        {
            OpeningAccount = true;
            await _apiClient.OpenNewAccountForClientAsync(_clientId, new OpenNewAccountForClientRequest { AccountName = NewAccountName });
            NewAccountName = string.Empty;
            OpeningAccount = false;
        });
    }

    [RelayCommand]
    private void OpenAccount(AccountReport? account)
    {
        if (account?.Id is { } id) _navigation.ShowAccountDetails(id);
    }

    // AssignNewBankCardForAccount (Fohjin.DDD.Domain/Client.cs) guards that the account belongs
    // to this client - only open accounts are ever in Client's own _accounts list, so only those
    // are offered here (docs/02-bank-cards.md: this whole feature has no read-model card
    // number/type, just an id + linked account + status - nothing here is invented beyond what
    // the domain has, matching Vue's and WinForms' equivalents).
    [RelayCommand]
    private async Task AssignNewBankCardAsync()
    {
        await RunSafelyAsync(async () =>
        {
            if (SelectedNewBankCardAccount is not { Id: var accountId }) return;
            AssigningBankCard = true;
            await _apiClient.AssignNewBankCardAsync(_clientId, new AssignNewBankCardRequest { AccountId = accountId });
            AssigningBankCard = false;
        });
    }

    [RelayCommand]
    private async Task CancelBankCardAsync(BankCardReport? card)
    {
        card ??= SelectedBankCard;
        if (card is null) return;
        await RunSafelyAsync(() => _apiClient.CancelBankCardAsync(_clientId, card.Id));
    }

    [RelayCommand]
    private async Task ReportBankCardStolenAsync(BankCardReport? card)
    {
        card ??= SelectedBankCard;
        if (card is null) return;
        await RunSafelyAsync(() => _apiClient.ReportStolenBankCardAsync(_clientId, card.Id));
    }

    // Which domain events should make this screen reload - same rule
    // Fohjin.DDD.WebUI/src/events/refreshRules.ts's shouldRefreshClientDetails encodes (client-
    // level events by this client's own id; the two bank-card "disabled" events by whether the
    // event's aggregate id matches one of the cards already loaded here, since those events
    // carry the BANK CARD's own id, not the client's - docs/02-bank-cards.md).
    private static readonly HashSet<string> ClientLevelEvents =
    [
        "ClientNameChangedEvent", "ClientMovedEvent", "ClientPhoneNumberChangedEvent",
        "AccountToClientAssignedEvent", "NewBankCardForAccountAsignedEvent",
    ];
    private static readonly HashSet<string> BankCardEvents =
        ["BankCardWasCanceledByClientEvent", "BankCardWasReportedStolenEvent"];

    public void OnDomainEvent(string eventType, Guid aggregateId)
    {
        var relevant =
            (ClientLevelEvents.Contains(eventType) && aggregateId == _clientId) ||
            (BankCardEvents.Contains(eventType) && BankCards.Any(c => c.Id == aggregateId));
        if (relevant) _ = LoadAsyncWithReconciliation();
    }

    // See Fohjin.DDD.WebUI/src/composables/useClientDetails.ts's watchLiveEvents for why this
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
