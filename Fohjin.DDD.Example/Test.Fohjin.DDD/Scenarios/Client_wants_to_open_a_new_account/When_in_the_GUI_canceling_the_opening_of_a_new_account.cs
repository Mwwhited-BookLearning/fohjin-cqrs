using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test.Fohjin.DDD.Scenarios.Client_wants_to_open_a_new_account;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_canceling_the_opening_of_a_new_account : PresenterTestFixture<ClientDetailsPresenter>
{
    private readonly Guid _clientId = Guid.NewGuid();
    private ClientDetailsReport _clientDetailsReport = null!;

    protected override void SetupDependencies()
    {
        _clientDetailsReport = new ClientDetailsReport
        {
            Id = _clientId,
            ClientName = "Client Name",
            Street = "street",
            StreetNumber = "123",
            PostalCode = "5000",
            City = "bergen",
            PhoneNumber = "1234567890",
        };
        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetClientDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(_clientDetailsReport);
    }

    protected override void Given()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateOpenNewAccount += delegate { });
        On<IClientDetailsView>().ValueFor(x => x.NewAccountName).IsSetTo("New account name");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
    }

    protected override void When()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnCancel += null);
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
    public void Then_disable_the_save_button()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableSaveButton()).WasCalled();
    }

    [TestMethod]
    public void Then_overview_panel_will_be_shown()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableOverviewPanel()).WasCalled();
    }
}