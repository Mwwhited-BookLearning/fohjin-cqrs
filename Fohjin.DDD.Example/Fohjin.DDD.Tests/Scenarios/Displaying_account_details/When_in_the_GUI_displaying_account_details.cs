using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fohjin.DDD.Tests.Scenarios.Displaying_account_details;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_displaying_account_details : PresenterTestFixture<AccountDetailsPresenter>
{
    private AccountDetailsReport _accountDetailsReport = null!;
    private List<AccountReport>? _accountReports;

    protected override void SetupDependencies()
    {
        _accountDetailsReport = new AccountDetailsReport { Id = Guid.NewGuid(), ClientReportId = Guid.NewGuid(), AccountName = "Account name", Balance = 10.5, AccountNumber = "1234567890" };

        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetAccountDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(_accountDetailsReport);

        var accountReport1 = new AccountReport { Id = Guid.NewGuid(), ClientDetailsReportId = Guid.NewGuid(), AccountName = "Account name 1", AccountNumber = "1234567890" };
        var accountReport2 = new AccountReport { Id = Guid.NewGuid(), ClientDetailsReportId = Guid.NewGuid(), AccountName = "Account name 2", AccountNumber = "1234567890" };
        _accountReports = new List<AccountReport> {accountReport1, accountReport2};

        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetAccountsAsync())
            .ReturnsAsync(_accountReports);
    }

    protected override void When()
    {
        Presenter?.SetAccount(new AccountReport { Id = Guid.NewGuid(), ClientDetailsReportId = Guid.NewGuid(), AccountName = "Account name", AccountNumber = "1234567890" });
        Presenter?.Display();
    }

    [TestMethod]
    public void Then_the_save_button_will_be_disabled()
    {
        On<IAccountDetailsView>().VerifyThat.Method(x => x.DisableSaveButton()).WasCalled();
    }

    [TestMethod]
    public void Then_the_menu_button_will_be_enabled()
    {
        On<IAccountDetailsView>().VerifyThat.Method(x => x.EnableMenuButtons()).WasCalled();
    }

    [TestMethod]
    public void Then_overview_panel_will_be_shown()
    {
        On<IAccountDetailsView>().VerifyThat.Method(x => x.EnableDetailsPanel()).WasCalled();
    }

    [TestMethod]
    public void Then_client_details_report_data_from_the_reporting_repository_is_being_loaded_into_the_view()
    {
        On<IAccountDetailsView>().VerifyThat.ValueIsSetFor(x => x.AccountName = _accountDetailsReport.AccountName);
        On<IAccountDetailsView>().VerifyThat.ValueIsSetFor(x => x.AccountNameLabel = _accountDetailsReport.AccountName);
        On<IAccountDetailsView>().VerifyThat.ValueIsSetFor(x => x.AccountNumberLabel = _accountDetailsReport.AccountNumber);
        On<IAccountDetailsView>().VerifyThat.ValueIsSetFor(x => x.BalanceLabel = (decimal)_accountDetailsReport.Balance);
        On<IAccountDetailsView>().VerifyThat.ValueIsSetFor(x => x.Ledgers = _accountDetailsReport.Ledgers);
        On<IAccountDetailsView>().VerifyThat.ValueIsSetFor(x => x.TransferAccounts = _accountReports);
    }

    [TestMethod]
    public void Then_show_dialog_will_be_called_on_the_view()
    {
        On<IAccountDetailsView>().VerifyThat.Method(x => x.ShowDialog()).WasCalled();
    }
}
