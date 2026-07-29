using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Common;

namespace Fohjin.DDD.BankApplication.Presenters;

public class ClientDetailsPresenter(
    IClientDetailsView clientDetailsView,
    IAccountDetailsPresenter accountDetailsPresenter,
    IPopupPresenter popupPresenter,
    FohjinApiClient apiClient,
    ISystemTimer systemTimer
        ) : Presenter<IClientDetailsView>(clientDetailsView), IClientDetailsPresenter
{
    private bool _createNewProcess = false;
    private bool _addNewAccountProcess = false;
    private bool _addNewBankCardProcess = false;
    private int _editStep = 0;
    private ClientReport? _clientReport;
    private ClientDetailsReport _clientDetailsReport = new();
    private readonly IClientDetailsView _clientDetailsView = clientDetailsView;
    private readonly IAccountDetailsPresenter _accountDetailsPresenter = accountDetailsPresenter;
    private readonly IPopupPresenter _popupPresenter = popupPresenter;
    private readonly FohjinApiClient _apiClient = apiClient;
    private readonly ISystemTimer _systemTimer = systemTimer;

    public async void Display()
    {
        _createNewProcess = false;
        _clientDetailsView.DisableSaveButton();
        DisableAllMenuButtons();
        _clientDetailsView.EnableOverviewPanel();

        if (_clientReport == null)
        {
            _editStep = 1;
            _createNewProcess = true;
            _clientDetailsReport = new ClientDetailsReport { Id = Guid.NewGuid() };
            ResetForm();
            _clientDetailsView.EnableClientNamePanel();
            _clientDetailsView.ShowDialog();
            return;
        }

        await LoadDataAsync();

        EnableAllMenuButtons();
        _clientDetailsView.ShowDialog();
    }

    private async Task LoadDataAsync()
    {
        _clientDetailsReport = await _apiClient.GetClientDetailsByIdAsync(_clientReport!.Id);

        SetClientDetailsData();
        SetReadOnlyData();
    }

    public void SetClient(ClientReport? clientReport)
    {
        _clientReport = clientReport;
    }

    public void OpenSelectedAccount()
    {
        _popupPresenter.CatchPossibleException(() =>
        {
            var account = _clientDetailsView.GetSelectedAccount();
            _accountDetailsPresenter.SetAccount(account);
            _accountDetailsPresenter.Display();
        });
    }

    public void FormElementGotChanged()
    {
        _clientDetailsView.DisableSaveButton();

        if (!FormIsValid())
            return;

        if (_createNewProcess)
        {
            _clientDetailsView.EnableSaveButton();
            return;
        }

        if (_addNewAccountProcess)
        {
            _clientDetailsView.EnableSaveButton();
            return;
        }

        if (_addNewBankCardProcess)
        {
            _clientDetailsView.EnableSaveButton();
            return;
        }

        if (FormHasChanged())
        {
            _clientDetailsView.EnableSaveButton();
            return;
        }
    }

    public async void SaveNewClientName()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            _clientDetailsView.DisableSaveButton();
            if (_createNewProcess)
            {
                _editStep = 2;
                _clientDetailsReport = new ClientDetailsReport
                {
                    Id = _clientDetailsReport.Id,
                    ClientName = _clientDetailsView.ClientName,
                };

                _clientDetailsView.EnableAddressPanel();
                return;
            }

            await _apiClient.ChangeClientNameAsync(_clientDetailsReport.Id, new ChangeClientNameRequest
            {
                ClientName = _clientDetailsView.ClientName,
            });

            _clientDetailsReport.ClientName = _clientDetailsView.ClientName;

            EnableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();
            _systemTimer.Trigger(LoadDataAsync, 1000);
        });
    }

    public async void SaveNewAddress()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            _clientDetailsView.DisableSaveButton();
            if (_createNewProcess)
            {
                _editStep = 3;

                _clientDetailsReport.Street = _clientDetailsView.Street;
                _clientDetailsReport.StreetNumber = _clientDetailsView.StreetNumber;
                _clientDetailsReport.PostalCode = _clientDetailsView.PostalCode;
                _clientDetailsReport.City = _clientDetailsView.City;

                _clientDetailsView.EnablePhoneNumberPanel();
                return;
            }

            await _apiClient.ChangeClientAddressAsync(_clientDetailsReport.Id, new ClientIsMovingRequest
            {
                Street = _clientDetailsView.Street,
                StreetNumber = _clientDetailsView.StreetNumber,
                PostalCode = _clientDetailsView.PostalCode,
                City = _clientDetailsView.City,
            });

            _clientDetailsReport.Street = _clientDetailsView.Street;
            _clientDetailsReport.StreetNumber = _clientDetailsView.StreetNumber;
            _clientDetailsReport.PostalCode = _clientDetailsView.PostalCode;
            _clientDetailsReport.City = _clientDetailsView.City;

            EnableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    public async void SaveNewPhoneNumber()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            _clientDetailsView.DisableSaveButton();
            if (_createNewProcess)
            {
                _editStep = 4;

                await _apiClient.CreateClientAsync(new CreateClientRequest
                {
                    ClientName = _clientDetailsReport.ClientName,
                    Street = _clientDetailsReport.Street,
                    StreetNumber = _clientDetailsReport.StreetNumber,
                    PostalCode = _clientDetailsReport.PostalCode,
                    City = _clientDetailsReport.City,
                    PhoneNumber = _clientDetailsView.PhoneNumber,
                });

                _clientDetailsView.Close();
                return;
            }

            await _apiClient.ChangeClientPhoneNumberAsync(_clientDetailsReport.Id, new ChangeClientPhoneNumberRequest
            {
                PhoneNumber = _clientDetailsView.PhoneNumber,
            });

            _clientDetailsReport.PhoneNumber = _clientDetailsView.PhoneNumber;

            EnableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    public async void CreateNewAccount()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            await _apiClient.OpenNewAccountForClientAsync(_clientDetailsReport.Id, new OpenNewAccountForClientRequest
            {
                AccountName = _clientDetailsView.NewAccountName,
            });

            _addNewAccountProcess = false;
            EnableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    // AssignNewBankCardForAccount (Fohjin.DDD.Domain/Client.cs) guards that the account belongs
    // to this client - only open accounts are ever in Client's own _accounts list, so only those
    // are offered here (docs/02-bank-cards.md: this whole feature has no read-model card
    // number/type, just an id + linked account + status - nothing here is invented beyond what
    // the domain has, matching Vue's ClientDetails.vue).
    public void InitiateAssignNewBankCard()
    {
        _editStep = 5;
        _addNewBankCardProcess = true;

        DisableAllMenuButtons();
        _clientDetailsView.EnableAddNewBankCardPanel();
    }

    public async void AssignNewBankCard()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            var account = _clientDetailsView.GetSelectedNewBankCardAccount();
            await _apiClient.AssignNewBankCardAsync(_clientDetailsReport.Id, new AssignNewBankCardRequest
            {
                AccountId = account!.Id,
            });

            _addNewBankCardProcess = false;
            EnableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();
            _systemTimer.Trigger(LoadDataAsync, 2000);
        });
    }

    // Bank-card cancel/report-stolen events carry the BANK CARD's own id as AggregateId, not the
    // client's (docs/02-bank-cards.md) - irrelevant here since these call the API directly by
    // the selected card's own Id, not by watching a live event stream the way Vue's client-side
    // event bus does.
    public void BankCardSelectionChanged()
    {
        var selected = _clientDetailsView.GetSelectedBankCard();
        if (selected is not null && selected.Status == "Active")
        {
            _clientDetailsView.EnableCancelBankCardButton();
            _clientDetailsView.EnableReportBankCardStolenButton();
            return;
        }

        _clientDetailsView.DisableCancelBankCardButton();
        _clientDetailsView.DisableReportBankCardStolenButton();
    }

    public async void CancelSelectedBankCard()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            var card = _clientDetailsView.GetSelectedBankCard();
            await _apiClient.CancelBankCardAsync(_clientDetailsReport.Id, card!.Id);
            _systemTimer.Trigger(LoadDataAsync, 1000);
        });
    }

    public async void ReportSelectedBankCardStolen()
    {
        await _popupPresenter.CatchPossibleExceptionAsync(async () =>
        {
            var card = _clientDetailsView.GetSelectedBankCard();
            await _apiClient.ReportStolenBankCardAsync(_clientDetailsReport.Id, card!.Id);
            _systemTimer.Trigger(LoadDataAsync, 1000);
        });
    }

    public void Cancel()
    {
        if (_createNewProcess)
        {
            _clientDetailsView.Close();
            return;
        }

        _addNewAccountProcess = false;
        _addNewBankCardProcess = false;
        EnableAllMenuButtons();
        _clientDetailsView.EnableOverviewPanel();
        _clientDetailsView.DisableSaveButton();
        SetClientDetailsData();
    }

    public void InitiateClientNameChange()
    {
        _editStep = 1;
        DisableAllMenuButtons();
        _clientDetailsView.EnableClientNamePanel();
    }

    public void InitiateClientHasMoved()
    {
        _editStep = 2;
        DisableAllMenuButtons();
        _clientDetailsView.EnableAddressPanel();
    }

    public void InitiateClientPhoneNumberChanged()
    {
        _editStep = 3;
        DisableAllMenuButtons();
        _clientDetailsView.EnablePhoneNumberPanel();
    }

    public void InitiateOpenNewAccount()
    {
        _editStep = 4;
        _addNewAccountProcess = true;

        _clientDetailsView.NewAccountName = string.Empty;

        DisableAllMenuButtons();
        _clientDetailsView.EnableAddNewAccountPanel();
    }

    private void SetReadOnlyData()
    {
        _clientDetailsView.ClientNameLabel = _clientDetailsReport.ClientName;
        _clientDetailsView.PhoneNumberLabel = _clientDetailsReport.PhoneNumber;
        _clientDetailsView.AddressLine1Label = string.Format("{0} {1}", _clientDetailsReport.Street, _clientDetailsReport.StreetNumber);
        _clientDetailsView.AddressLine2Label = string.Format("{0} {1}", _clientDetailsReport.PostalCode, _clientDetailsReport.City);
    }

    private void ResetForm()
    {
        _clientDetailsView.ClientName = string.Empty;
        _clientDetailsView.Street = string.Empty;
        _clientDetailsView.StreetNumber = string.Empty;
        _clientDetailsView.PostalCode = string.Empty;
        _clientDetailsView.City = string.Empty;
        _clientDetailsView.PhoneNumber = string.Empty;
        _clientDetailsView.Accounts = null;
        _clientDetailsView.ClosedAccounts = null;
        _clientDetailsView.BankCards = null;
    }

    private void DisableAllMenuButtons()
    {
        _clientDetailsView.DisableAddNewAccountMenu();
        _clientDetailsView.DisableClientHasMovedMenu();
        _clientDetailsView.DisableNameChangedMenu();
        _clientDetailsView.DisablePhoneNumberChangedMenu();
        _clientDetailsView.DisableAddNewBankCardMenu();
    }

    private void SetClientDetailsData()
    {
        _clientDetailsView.ClientName = _clientDetailsReport.ClientName;
        _clientDetailsView.Street = _clientDetailsReport.Street;
        _clientDetailsView.StreetNumber = _clientDetailsReport.StreetNumber;
        _clientDetailsView.PostalCode = _clientDetailsReport.PostalCode;
        _clientDetailsView.City = _clientDetailsReport.City;
        _clientDetailsView.PhoneNumber = _clientDetailsReport.PhoneNumber;
        _clientDetailsView.Accounts = _clientDetailsReport.Accounts;
        _clientDetailsView.ClosedAccounts = _clientDetailsReport.ClosedAccounts;
        _clientDetailsView.BankCards = _clientDetailsReport.BankCards;
    }

    private void EnableAllMenuButtons()
    {
        _clientDetailsView.EnableAddNewAccountMenu();
        _clientDetailsView.EnableClientHasMovedMenu();
        _clientDetailsView.EnableNameChangedMenu();
        _clientDetailsView.EnablePhoneNumberChangedMenu();
        _clientDetailsView.EnableAddNewBankCardMenu();
    }

    private bool FormIsValid()
    {
        if (_editStep == 0)
            return true;

        if (_editStep == 1)
            return !string.IsNullOrEmpty(_clientDetailsView.ClientName);

        if (_editStep == 2)
            return
                !string.IsNullOrEmpty(_clientDetailsView.Street) &&
                !string.IsNullOrEmpty(_clientDetailsView.StreetNumber) &&
                !string.IsNullOrEmpty(_clientDetailsView.PostalCode) &&
                !string.IsNullOrEmpty(_clientDetailsView.City);

        if (_editStep == 3)
            return !string.IsNullOrEmpty(_clientDetailsView.PhoneNumber);

        if (_editStep == 4)
            return
                !string.IsNullOrEmpty(_clientDetailsView.NewAccountName);

        if (_editStep == 5)
            return _clientDetailsView.GetSelectedNewBankCardAccount() != null;

        throw new Exception("Edit step was not properly initialized!");
    }

    private bool FormHasChanged()
    {
        return
            AddressHasChanged() ||
            PhoneNumberHasChanged() ||
            ClientNameHasChanged();
    }

    private bool AddressHasChanged()
    {
        return
            _clientDetailsView.Street != _clientDetailsReport.Street ||
            _clientDetailsView.StreetNumber != _clientDetailsReport.StreetNumber ||
            _clientDetailsView.PostalCode != _clientDetailsReport.PostalCode ||
            _clientDetailsView.City != _clientDetailsReport.City;
    }

    private bool PhoneNumberHasChanged()
    {
        return _clientDetailsView.PhoneNumber != _clientDetailsReport.PhoneNumber;
    }

    private bool ClientNameHasChanged()
    {
        return _clientDetailsView.ClientName != _clientDetailsReport.ClientName;
    }
}
