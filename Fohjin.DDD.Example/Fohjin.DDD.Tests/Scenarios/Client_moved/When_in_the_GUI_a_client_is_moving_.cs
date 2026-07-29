using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fohjin.DDD.Tests.Scenarios.Client_moved;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_a_client_is_moving_ : PresenterTestFixture<ClientDetailsPresenter>
{
    private readonly Guid _clientId = Guid.NewGuid();
    private ClientDetailsReport _clientDetailsReport = null!;

    protected override void SetupDependencies()
    {
        _clientDetailsReport = new ClientDetailsReport
        {
            Id = _clientId,
            ClientName = "Client Name",
            Street = "Street",
            StreetNumber = "123",
            PostalCode = "5000",
            City = "Bergen",
            PhoneNumber = "1234567890",
        };
        OnDependency<FohjinApiClient>()
            .Setup(x => x.GetClientDetailsByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(_clientDetailsReport);
    }

    protected override void Given()
    {
        Presenter.SetClient(new ClientReport { Id = _clientId, Name = "Client Name" });
        Presenter.Display();
    }

    protected override void When()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateClientHasMoved += null);
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
    public void Then_the_name_change_panel_will_be_enabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableAddressPanel()).WasCalled();
    }
}