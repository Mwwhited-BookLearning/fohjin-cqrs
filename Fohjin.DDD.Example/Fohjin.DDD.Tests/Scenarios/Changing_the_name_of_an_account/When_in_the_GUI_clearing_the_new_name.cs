using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fohjin.DDD.Tests.Scenarios.Changing_the_name_of_an_account;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_clearing_the_new_name : PresenterTestFixture<AccountDetailsPresenter>
{
    protected override void SetupDependencies()
    {
        OnDependency<IPopupPresenter>()
            .Setup(x => x.CatchPossibleExceptionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

        var accountDetailsReport = new AccountDetailsReport { Id = Guid.NewGuid(), ClientReportId = Guid.NewGuid(), AccountName = "Account name", Balance = 10.5, AccountNumber = "1234567890" };
        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetAccountDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(accountDetailsReport);

        var accountReports = new List<AccountReport> { new AccountReport { Id = Guid.NewGuid(), ClientDetailsReportId = Guid.NewGuid(), AccountName = "Account name 1", AccountNumber = "1234567890" } };
        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetAccountsAsync())
            .ReturnsAsync(accountReports);
    }

    protected override void Given()
    {
        Presenter.SetAccount(new AccountReport { Id = Guid.NewGuid(), ClientDetailsReportId = Guid.NewGuid(), AccountName = "Account name", AccountNumber = "1234567890" });
        Presenter.Display();
        On<IAccountDetailsView>().FireEvent(x => x.OnInitiateAccountNameChange += null);
        On<IAccountDetailsView>().ValueFor(x => x.AccountName).IsSetTo("New Account name");
        On<IAccountDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
    }

    protected override void When()
    {
        On<IAccountDetailsView>().ValueFor(x => x.AccountName).IsSetTo(string.Empty);
        On<IAccountDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
    }

    [TestMethod]
    public void Then_the_save_button_will_be_enabled()
    {
        On<IAccountDetailsView>().VerifyThat.Method(x => x.DisableSaveButton()).WasCalled();
    }
}
