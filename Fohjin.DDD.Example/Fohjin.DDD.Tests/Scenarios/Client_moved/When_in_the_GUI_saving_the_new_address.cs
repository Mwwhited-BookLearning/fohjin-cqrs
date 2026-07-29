using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace Fohjin.DDD.Tests.Scenarios.Client_moved;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_saving_the_new_address : PresenterTestFixture<ClientDetailsPresenter>
{
    private readonly Guid _clientId = Guid.NewGuid();
    private ClientDetailsReport _clientDetailsReport = null!;

    protected override void SetupDependencies()
    {
        OnDependency<IPopupPresenter>()
            .Setup(x => x.CatchPossibleExceptionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

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

        OnDependency<FohjinApiClient>()
            .Setup(x => x.ChangeClientAddressAsync(It.IsAny<Guid>(), It.IsAny<ClientIsMovingRequest>()))
            .Returns(Task.CompletedTask);
    }

    protected override void Given()
    {
        Presenter.SetClient(new ClientReport { Id = _clientId, Name = "Client Name" });
        Presenter.Display();
        On<IClientDetailsView>().ValueFor(x => x.ClientName).IsSetTo("Client name");
        On<IClientDetailsView>().ValueFor(x => x.PhoneNumber).IsSetTo("1234567890");
        On<IClientDetailsView>().ValueFor(x => x.Street).IsSetTo("Lane");
        On<IClientDetailsView>().ValueFor(x => x.StreetNumber).IsSetTo("321");
        On<IClientDetailsView>().ValueFor(x => x.PostalCode).IsSetTo("6000");
        On<IClientDetailsView>().ValueFor(x => x.City).IsSetTo("Oslo");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateClientPhoneNumberChanged += null);
    }

    protected override void When()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnSaveNewAddress += null);
    }

    [TestMethod]
    public void Then_a_change_client_phone_number_command_will_be_published()
    {
        On<FohjinApiClient>().VerifyThat.Method(x => x.ChangeClientAddressAsync(It.IsAny<Guid>(), It.IsAny<ClientIsMovingRequest>())).WasCalled();
    }

    [TestMethod]
    public void Then_the_save_button_will_be_disabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableSaveButton()).WasCalled();
    }

    [TestMethod]
    public void Then_the_menu_button_will_be_enabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableAddNewAccountMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableClientHasMovedMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableNameChangedMenu()).WasCalled();
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnablePhoneNumberChangedMenu()).WasCalled();
    }

    [TestMethod]
    public void Then_the_details_panel_will_be_enabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableOverviewPanel()).WasCalled();
    }
}