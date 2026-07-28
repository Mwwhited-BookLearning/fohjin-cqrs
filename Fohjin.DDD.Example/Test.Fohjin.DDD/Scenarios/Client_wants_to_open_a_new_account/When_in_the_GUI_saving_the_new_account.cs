using System;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace Test.Fohjin.DDD.Scenarios.Client_wants_to_open_a_new_account;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_saving_the_new_account : PresenterTestFixture<ClientDetailsPresenter>
{
    protected override void SetupDependencies()
    {
        OnDependency<IPopupPresenter>()
            .Setup(x => x.CatchPossibleExceptionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetClientDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new ClientDetailsReport
            {
                Id = Guid.NewGuid(),
                ClientName = "Client Name",
                Street = "street",
                StreetNumber = "123",
                PostalCode = "5000",
                City = "bergen",
                PhoneNumber = "1234567890",
            });

        OnDependency<FohjinApiClient>()
            .Setup(x => x.OpenNewAccountForClientAsync(It.IsAny<Guid>(), It.IsAny<OpenNewAccountForClientRequest>()))
            .Returns(Task.CompletedTask);
    }

    protected override void Given()
    {
        Presenter.SetClient(new ClientReport { Id = Guid.NewGuid(), Name = "Client name" });
        Presenter.Display();
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateOpenNewAccount += delegate { });
        On<IClientDetailsView>().ValueFor(x => x.NewAccountName).IsSetTo("New account name");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
    }

    protected override void When()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnCreateNewAccount += null);
    }

    [TestMethod]
    public void Then_a_add_new_account_to_client_command_will_be_published()
    {
        On<FohjinApiClient>().VerifyThat.Method(x => x.OpenNewAccountForClientAsync(It.IsAny<Guid>(), It.IsAny<OpenNewAccountForClientRequest>())).WasCalled();
    }

    [TestMethod]
    public void Then_the_menu_buttons_will_be_enabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableAddNewAccountMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableClientHasMovedMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableNameChangedMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnablePhoneNumberChangedMenu()).WasCalled();
    }

    [TestMethod]
    public void Then_overview_panel_will_be_shown()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableOverviewPanel()).WasCalled();
    }
}