using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fohjin.DDD.Tests.Scenarios.Client_got_a_new_phone_number;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_inserting_the_new_phone_number : PresenterTestFixture<ClientDetailsPresenter>
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
        On<IClientDetailsView>().ValueFor(x => x.ClientName).IsSetTo("Client name");
        On<IClientDetailsView>().ValueFor(x => x.PhoneNumber).IsSetTo("1234567890");
        On<IClientDetailsView>().ValueFor(x => x.Street).IsSetTo("Street");
        On<IClientDetailsView>().ValueFor(x => x.StreetNumber).IsSetTo("123");
        On<IClientDetailsView>().ValueFor(x => x.PostalCode).IsSetTo("5000");
        On<IClientDetailsView>().ValueFor(x => x.City).IsSetTo("Bergen");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateClientPhoneNumberChanged += null);
    }

    protected override void When()
    {
        On<IClientDetailsView>().ValueFor(x => x.PhoneNumber).IsSetTo("0987654321");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
    }

    [TestMethod]
    public void Then_the_save_button_will_be_disabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableSaveButton()).WasCalled();
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
    public void Then_the_save_button_will_be_enabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.EnableSaveButton()).WasCalled();
    }
}