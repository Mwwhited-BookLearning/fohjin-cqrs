using System;
using System.Collections.Generic;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test.Fohjin.DDD.Scenarios.Changing_the_name_of_an_account
{
    [TestClass]
    [TestCategory("unit")]
    public class When_in_the_GUI_inserting_the_new_name : PresenterTestFixture<AccountDetailsPresenter>
    {
        protected override void SetupDependencies()
        {
            OnDependency<IPopupPresenter>()
                .Setup(x => x.CatchPossibleException(It.IsAny<Action>()))
                .Callback<Action>(x => x());

            var accountDetailsReports = new List<AccountDetailsReport> { new AccountDetailsReport(Guid.NewGuid(), Guid.NewGuid(), "Account name", 10.5M, "1234567890") };
            OnDependency<IReportingRepository>()
                .Setup(x => x.GetByExampleAsync<AccountDetailsReport>(It.IsAny<object>()))
                .ReturnsAsync(accountDetailsReports);

            var accountReports = new List<AccountReport> { new AccountReport(Guid.NewGuid(), Guid.NewGuid(), "Account name 1", "1234567890") };
            OnDependency<IReportingRepository>()
                .Setup(x => x.GetByExampleAsync<AccountReport>(It.IsAny<object>()))
                .ReturnsAsync(accountReports);
        }

        protected override void Given()
        {
            Presenter.SetAccount(new AccountReport(Guid.NewGuid(), Guid.NewGuid(), "Account name", "1234567890"));
            Presenter.Display();
            On<IAccountDetailsView>().FireEvent(x => x.OnInitiateAccountNameChange += null);
        }

        protected override void When()
        {
            On<IAccountDetailsView>().ValueFor(x => x.AccountName).IsSetTo("New Account name");
            On<IAccountDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
        }

        [TestMethod]
        public void Then_the_save_button_will_be_enabled()
        {
            On<IAccountDetailsView>().VerifyThat.Method(x => x.EnableSaveButton()).WasCalled();
        }
    }
}