using System;
using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fohjin.DDD.Tests.Scenarios.Client_wants_to_open_a_new_account;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_opening_a_new_account : PresenterTestFixture<ClientDetailsPresenter>
{
    protected override void SetupDependencies()
    {
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
    }

    protected override void When()
    {
        Presenter.SetClient(new ClientReport { Id = Guid.NewGuid(), Name = "Client name" });
        Presenter.Display();
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateOpenNewAccount += delegate { });
    }

    [TestMethod]
    public void Then_the_menu_buttons_will_be_disabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableAddNewAccountMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableClientHasMovedMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableNameChangedMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisablePhoneNumberChangedMenu()).WasCalled();
    }

    [TestMethod]
    public void Then_the_add_new_panel_will_be_enabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableAddNewAccountPanel()).WasCalled();
    }
}