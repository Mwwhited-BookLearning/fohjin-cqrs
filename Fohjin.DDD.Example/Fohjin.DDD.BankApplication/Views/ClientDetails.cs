using Fohjin.DDD.ApiClient;
using System.ComponentModel;

namespace Fohjin.DDD.BankApplication.Views;

public partial class ClientDetails : ViewFormBase, IClientDetailsView
{
    public ClientDetails()
    {
        InitializeComponent();
        tabControl1.Appearance = TabAppearance.FlatButtons;
        tabControl1.ItemSize = new Size(0, 1);
        tabControl1.SizeMode = TabSizeMode.Fixed;
        RegisterClientEvents();
    }

    public event Action? OnOpenSelectedAccount;
    public event Action? OnFormElementGotChanged;
    public event Action? OnCancel;
    public event Action? OnSaveNewClientName;
    public event Action? OnSaveNewPhoneNumber;
    public event Action? OnSaveNewAddress;
    public event Action? OnInitiateClientHasMoved;
    public event Action? OnInitiateClientNameChange;
    public event Action? OnInitiateClientPhoneNumberChanged;
    public event Action? OnInitiateOpenNewAccount;
    public event Action? OnCreateNewAccount;
    public event Action? OnInitiateAssignNewBankCard;
    public event Action? OnAssignNewBankCard;
    public event Action? OnBankCardSelectionChanged;
    public event Action? OnCancelSelectedBankCard;
    public event Action? OnReportSelectedBankCardStolen;

    private void RegisterClientEvents()
    {
        nameChangedToolStripMenuItem.Click += (s, e) => OnInitiateClientNameChange?.Invoke();
        hasMovedToolStripMenuItem.Click += (s, e) => OnInitiateClientHasMoved?.Invoke();
        changedHisPhoneNumberToolStripMenuItem.Click += (s, e) => OnInitiateClientPhoneNumberChanged?.Invoke();
        addNewAccountToolStripMenuItem.Click += (s, e) => OnInitiateOpenNewAccount?.Invoke();
        _newAccountCreateButton.Click += (s, e) => OnCreateNewAccount?.Invoke();
        _newAccountCancelButton.Click += (s, e) => OnCancel?.Invoke();
        _clientNameSaveButton.Click += (s, e) => OnSaveNewClientName?.Invoke();
        _clientNameCancelButton.Click += (s, e) => OnCancel?.Invoke();
        _accounts.DoubleClick += (s, e) => OnOpenSelectedAccount?.Invoke();
        _addressCancelButton.Click += (s, e) => OnCancel?.Invoke();
        _addressSaveButton.Click += (s, e) => OnSaveNewAddress?.Invoke();
        _phoneNumberCancelButton.Click += (s, e) => OnCancel?.Invoke();
        _phoneNumberSaveButton.Click += (s, e) => OnSaveNewPhoneNumber?.Invoke();
        addNewBankCardToolStripMenuItem.Click += (s, e) => OnInitiateAssignNewBankCard?.Invoke();
        _newBankCardAssignButton.Click += (s, e) => OnAssignNewBankCard?.Invoke();
        _newBankCardCancelButton.Click += (s, e) => OnCancel?.Invoke();
        _newBankCardAccount.SelectedIndexChanged += (s, e) => OnFormElementGotChanged?.Invoke();
        _bankCards.SelectedIndexChanged += (s, e) => OnBankCardSelectionChanged?.Invoke();
        _cancelBankCardButton.Click += (s, e) => OnCancelSelectedBankCard?.Invoke();
        _reportBankCardStolenButton.Click += (s, e) => OnReportSelectedBankCardStolen?.Invoke();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? ClientName
    {
        get { return _clientName.Text; }
        set { _clientName.Text = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Street
    {
        get { return _street.Text; }
        set { _street.Text = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? StreetNumber
    {
        get { return _streetNumber.Text; }
        set { _streetNumber.Text = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? PostalCode
    {
        get { return _postalCode.Text; }
        set { _postalCode.Text = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? City
    {
        get { return _city.Text; }
        set { _city.Text = value; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<AccountReport>? Accounts
    {
        get { return _accounts.DataSource as IEnumerable<AccountReport>; }
        set
        {
            _accounts.DataSource = value;
            // AssignNewBankCardForAccount only accepts one of this client's own open accounts
            // (docs/02-bank-cards.md) - reusing the same list keeps that restriction implicit
            // rather than needing a second lookup.
            _newBankCardAccount.DataSource = value?.ToList();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<ClosedAccountReport>? ClosedAccounts
    {
        get { return _closedAccounts.DataSource as IEnumerable<ClosedAccountReport>; }
        set { _closedAccounts.DataSource = value; }
    }

    public AccountReport? GetSelectedAccount() =>
        _accounts.SelectedItem as AccountReport;

    public ClosedAccountReport? GetSelectedClosedAccount() =>
        _closedAccounts.SelectedItem as ClosedAccountReport;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<BankCardReport>? BankCards
    {
        get { return _bankCards.DataSource as IEnumerable<BankCardReport>; }
        set { _bankCards.DataSource = value; }
    }

    public BankCardReport? GetSelectedBankCard() =>
        _bankCards.SelectedItem as BankCardReport;

    public AccountReport? GetSelectedNewBankCardAccount() =>
        _newBankCardAccount.SelectedItem as AccountReport;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? PhoneNumber
    {
        get => _phoneNumber.Text;
        set => _phoneNumber.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? NewAccountName
    {
        get => _newAccountName.Text;
        set => _newAccountName.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? ClientNameLabel
    {
        set => _clientNameLabel.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? AddressLine1Label
    {
        set => _addressLine1Label.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? AddressLine2Label
    {
        set => _addressLine2Label.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? PhoneNumberLabel
    {
        set => _phoneNumberLabel.Text = value;
    }

    public void DisableAddNewAccountMenu()
    {
        addNewAccountToolStripMenuItem.Enabled = false;
    }

    public void EnableClientHasMovedMenu()
    {
        hasMovedToolStripMenuItem.Enabled = true;
    }

    public void DisableClientHasMovedMenu()
    {
        hasMovedToolStripMenuItem.Enabled = false;
    }

    public void EnableNameChangedMenu()
    {
        nameChangedToolStripMenuItem.Enabled = true;
    }

    public void DisableNameChangedMenu()
    {
        nameChangedToolStripMenuItem.Enabled = false;
    }

    public void EnablePhoneNumberChangedMenu()
    {
        changedHisPhoneNumberToolStripMenuItem.Enabled = true;
    }

    public void DisablePhoneNumberChangedMenu()
    {
        changedHisPhoneNumberToolStripMenuItem.Enabled = false;
    }

    public void EnableAddNewAccountMenu()
    {
        addNewAccountToolStripMenuItem.Enabled = true;
    }

    public void EnableAddNewBankCardMenu()
    {
        addNewBankCardToolStripMenuItem.Enabled = true;
    }

    public void DisableAddNewBankCardMenu()
    {
        addNewBankCardToolStripMenuItem.Enabled = false;
    }

    public void EnableCancelBankCardButton()
    {
        _cancelBankCardButton.Enabled = true;
    }

    public void DisableCancelBankCardButton()
    {
        _cancelBankCardButton.Enabled = false;
    }

    public void EnableReportBankCardStolenButton()
    {
        _reportBankCardStolenButton.Enabled = true;
    }

    public void DisableReportBankCardStolenButton()
    {
        _reportBankCardStolenButton.Enabled = false;
    }

    public void EnableSaveButton()
    {
        _addressSaveButton.Enabled = true;
        _phoneNumberSaveButton.Enabled = true;
        _clientNameSaveButton.Enabled = true;
        _newAccountCreateButton.Enabled = true;
        _newBankCardAssignButton.Enabled = true;
    }

    public void DisableSaveButton()
    {
        _addressSaveButton.Enabled = false;
        _phoneNumberSaveButton.Enabled = false;
        _clientNameSaveButton.Enabled = false;
        _newAccountCreateButton.Enabled = false;
        _newBankCardAssignButton.Enabled = false;
    }

    public void EnableOverviewPanel()
    {
        tabControl1.SelectedIndex = 0;
    }

    public void EnableAddressPanel()
    {
        tabControl1.SelectedIndex = 1;
        _street.Focus();
    }

    public void EnablePhoneNumberPanel()
    {
        tabControl1.SelectedIndex = 2;
        _phoneNumber.Focus();
    }

    public void EnableClientNamePanel()
    {
        tabControl1.SelectedIndex = 3;
        _clientName.Focus();
    }

    public void EnableAddNewAccountPanel()
    {
        tabControl1.SelectedIndex = 4;
        _newAccountName.Focus();
    }

    public void EnableAddNewBankCardPanel()
    {
        tabControl1.SelectedIndex = 5;
        _newBankCardAccount.Focus();
    }

    private void ClientChanged(object sender, EventArgs e) =>
        OnFormElementGotChanged?.Invoke();
}