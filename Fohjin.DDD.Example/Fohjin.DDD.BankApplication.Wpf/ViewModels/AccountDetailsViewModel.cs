using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Wpf.Services;

namespace Fohjin.DDD.BankApplication.Wpf.ViewModels;

public partial class AccountDetailsViewModel : ViewModelBase
{
    private readonly FohjinApiClient _apiClient;
    private readonly INavigationService _navigation;
    private Guid _accountId;

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private string accountName = string.Empty;
    [ObservableProperty]
    private string? accountNumber;
    [ObservableProperty]
    private double balance;

    [ObservableProperty]
    private double depositAmount;
    [ObservableProperty]
    private double withdrawalAmount;
    [ObservableProperty]
    private double transferAmount;
    [ObservableProperty]
    private AccountReport? transferTargetAccount;

    [ObservableProperty]
    private bool savingName;
    [ObservableProperty]
    private bool depositing;
    [ObservableProperty]
    private bool withdrawing;
    [ObservableProperty]
    private bool transferring;
    [ObservableProperty]
    private bool closing;

    public ObservableCollection<LedgerReport> Ledgers { get; } = [];
    public ObservableCollection<AccountReport> OtherAccounts { get; } = [];

    public AccountDetailsViewModel(FohjinApiClient apiClient, INavigationService navigation)
    {
        _apiClient = apiClient;
        _navigation = navigation;
    }

    public void Initialize(Guid accountId) => _accountId = accountId;

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunSafelyAsync(async () =>
        {
            IsLoading = true;
            var details = await _apiClient.GetAccountDetailsByIdAsync(_accountId);
            AccountName = details.AccountName ?? "";
            AccountNumber = details.AccountNumber;
            Balance = details.Balance;

            Ledgers.Clear();
            foreach (var l in details.Ledgers ?? []) Ledgers.Add(l);

            var allAccounts = await _apiClient.GetAccountsAsync();
            OtherAccounts.Clear();
            foreach (var a in allAccounts.Where(a => a.Id != _accountId)) OtherAccounts.Add(a);

            IsLoading = false;
        });
    }

    [RelayCommand]
    private async Task SaveNameAsync()
    {
        await RunSafelyAsync(async () =>
        {
            SavingName = true;
            await _apiClient.ChangeAccountNameAsync(_accountId, new ChangeAccountNameRequest { AccountName = AccountName });
            SavingName = false;
        });
    }

    [RelayCommand]
    private async Task DepositAsync()
    {
        await RunSafelyAsync(async () =>
        {
            Depositing = true;
            await _apiClient.DepositCashAsync(_accountId, new DepositCashRequest { Amount = DepositAmount });
            DepositAmount = 0;
            Depositing = false;
        });
    }

    [RelayCommand]
    private async Task WithdrawAsync()
    {
        await RunSafelyAsync(async () =>
        {
            Withdrawing = true;
            await _apiClient.WithdrawalCashAsync(_accountId, new WithdrawalCashRequest { Amount = WithdrawalAmount });
            WithdrawalAmount = 0;
            Withdrawing = false;
        });
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        await RunSafelyAsync(async () =>
        {
            if (TransferTargetAccount is null) return;
            Transferring = true;
            await _apiClient.SendMoneyTransferAsync(_accountId, new SendMoneyTransferRequest
            {
                Amount = TransferAmount,
                AccountNumber = TransferTargetAccount.AccountNumber,
            });
            TransferAmount = 0;
            Transferring = false;
        });
    }

    [RelayCommand]
    private async Task CloseAccountAsync()
    {
        await RunSafelyAsync(async () =>
        {
            Closing = true;
            await _apiClient.CloseAccountAsync(_accountId);
            Closing = false;
            _navigation.ShowClientSearch();
        });
    }

    // Which domain events should make this screen reload - same rule
    // Fohjin.DDD.WebUI/src/events/refreshRules.ts's shouldRefreshAccountDetails encodes: every
    // one of these is applied on the ActiveAccount aggregate itself, so AggregateId is always
    // this account's own id regardless of whether this account initiated the change or is only
    // the target of someone else's transfer.
    private static readonly HashSet<string> RelevantEvents =
    [
        "AccountNameChangedEvent", "CashDepositedEvent", "CashWithdrawnEvent",
        "MoneyTransferSendEvent", "MoneyTransferReceivedEvent", "MoneyTransferFailedEvent",
    ];

    public void OnDomainEvent(string eventType, Guid aggregateId)
    {
        if (RelevantEvents.Contains(eventType) && aggregateId == _accountId) _ = LoadAsyncWithReconciliation();
    }

    private async Task LoadAsyncWithReconciliation()
    {
        await LoadCommand.ExecuteAsync(null);
        await Task.Delay(750);
        await LoadCommand.ExecuteAsync(null);
    }
}
