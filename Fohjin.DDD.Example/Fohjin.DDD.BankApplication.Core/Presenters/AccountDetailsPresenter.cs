using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Common;

namespace Fohjin.DDD.BankApplication.Presenters;

public class AccountDetailsPresenter(
    IAccountDetailsView accountDetailsView,
    IPopupPresenter popupPresenter,
    FohjinApiClient apiClient,
    ISystemTimer systemTimer) : Presenter<IAccountDetailsView>(accountDetailsView), IAccountDetailsPresenter
{
    private int _editStep = 0;
    private AccountReport? _accountReport;
    private AccountDetailsReport _accountDetailsReport = new();
    private readonly IAccountDetailsView _accountDetailsView = accountDetailsView;
    private readonly IPopupPresenter _popupPresenter = popupPresenter;
    private readonly FohjinApiClient _apiClient = apiClient;
    private readonly ISystemTimer _systemTimer = systemTimer;

    public async void Display()
    {
        _accountDetailsView.DisableSaveButton();
        _accountDetailsView.EnableMenuButtons();
        _accountDetailsView.EnableDetailsPanel();

        await LoadDataAsync();
        _accountDetailsView.ShowDialog();
    }

    private async Task LoadDataAsync()
    {
        if (_accountReport == null)
            return;

        _accountDetailsReport = await _apiClient.GetAccountDetailsByIdAsync(_accountReport.Id);
        _accountDetailsView.AccountName = _accountDetailsReport.AccountName;
        _accountDetailsView.AccountNameLabel = _accountDetailsReport.AccountName;
        _accountDetailsView.AccountNumberLabel = _accountDetailsReport.AccountNumber;
        _accountDetailsView.BalanceLabel = (decimal)_accountDetailsReport.Balance;
        _accountDetailsView.Ledgers = _accountDetailsReport.Ledgers;
        _accountDetailsView.TransferAccounts = [.. (await _apiClient.GetAccountsAsync()).Where(x => x.Id != _accountDetailsReport.Id)];
    }

    public void SetAccount(AccountReport? accountReport)
    {
        _accountReport = accountReport;
    }

    public async void CloseTheAccount()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            if (_accountReport != null)
                await _apiClient.CloseAccountAsync(_accountReport.Id);

            _accountDetailsView.Close();
        });
    }

    public void Cancel()
    {
        _editStep = 0;
        _accountDetailsView.EnableDetailsPanel();
        _accountDetailsView.DisableSaveButton();
        _accountDetailsView.EnableMenuButtons();
    }

    public void InitiateMoneyDeposit()
    {
        _editStep = 1;
        _accountDetailsView.DepositAmount = 0M;
        _accountDetailsView.DisableMenuButtons();
        _accountDetailsView.EnableDepositPanel();
    }

    public void InitiateMoneyWithdrawal()
    {
        _editStep = 2;
        _accountDetailsView.WithdrawalAmount = 0M;
        _accountDetailsView.DisableMenuButtons();
        _accountDetailsView.EnableWithdrawalPanel();
    }

    public void InitiateMoneyTransfer()
    {
        _editStep = 3;
        _accountDetailsView.TransferAmount = 0M;
        _accountDetailsView.DisableMenuButtons();
        _accountDetailsView.EnableTransferPanel();
    }

    public void InitiateAccountNameChange()
    {
        _editStep = 4;
        _accountDetailsView.AccountName = _accountDetailsReport.AccountName;
        _accountDetailsView.DisableMenuButtons();
        _accountDetailsView.EnableAccountNameChangePanel();
    }

    public async void ChangeAccountName()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            await _apiClient.ChangeAccountNameAsync(_accountDetailsReport.Id, new ChangeAccountNameRequest
            {
                AccountName = _accountDetailsView.AccountName,
            });

            _accountDetailsReport.AccountName = _accountDetailsView.AccountName;

            _accountDetailsView.EnableMenuButtons();
            _accountDetailsView.EnableDetailsPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    public async void DepositMoney()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            await _apiClient.DepositCashAsync(_accountDetailsReport.Id, new DepositCashRequest
            {
                Amount = (double)_accountDetailsView.DepositAmount,
            });

            _accountDetailsView.EnableMenuButtons();
            _accountDetailsView.EnableDetailsPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    public async void WithdrawalMoney()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            await _apiClient.WithdrawalCashAsync(_accountDetailsReport.Id, new WithdrawalCashRequest
            {
                Amount = (double)_accountDetailsView.WithdrawalAmount,
            });

            _accountDetailsView.EnableMenuButtons();
            _accountDetailsView.EnableDetailsPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    public async void TransferMoney()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            await _apiClient.SendMoneyTransferAsync(_accountDetailsReport.Id, new SendMoneyTransferRequest
            {
                Amount = (double)_accountDetailsView.TransferAmount,
                AccountNumber = _accountDetailsView.GetSelectedTransferAccount()?.AccountNumber,
            });

            _accountDetailsView.EnableMenuButtons();
            _accountDetailsView.EnableDetailsPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
            _systemTimer.Trigger(LoadDataAsync, 4000); // This one is because there is also a delay in the transfer service :)
        });
    }

    public void FormElementGotChanged()
    {
        _accountDetailsView.DisableSaveButton();

        if (!FormIsValid())
            return;

        if (FormHasChanged())
        {
            _accountDetailsView.EnableSaveButton();
            return;
        }
    }

    private bool FormIsValid()
    {
        if (_editStep == 0 ||
            _editStep == 1 ||
            _editStep == 2 ||
            _editStep == 3)
            return true;

        if (_editStep == 4)
            return !string.IsNullOrEmpty(_accountDetailsView.AccountName);

        throw new Exception("Edit step was not properly initialized!");
    }

    private bool FormHasChanged()
    {
        return
            AccountNameHasChanged() ||
            DepositAmountHasChanged() ||
            WithdrawalAmountHasChanged() ||
            TransferAmountHasChanged();
    }

    private bool TransferAmountHasChanged()
    {
        return _accountDetailsView.TransferAmount > decimal.Zero;
    }

    private bool WithdrawalAmountHasChanged()
    {
        return _accountDetailsView.WithdrawalAmount > decimal.Zero;
    }

    private bool DepositAmountHasChanged()
    {
        return _accountDetailsView.DepositAmount > decimal.Zero;
    }

    private bool AccountNameHasChanged() => _accountDetailsView.AccountName != _accountDetailsReport?.AccountName;
}
