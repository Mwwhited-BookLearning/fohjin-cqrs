using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace Test.Fohjin.DDD.Scenarios.Adding_a_new_client;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_the_phone_number_of_the_new_client_is_saved : PresenterTestFixture<ClientDetailsPresenter>
{
    private CreateClientRequest CreateClientCommand = null!;

    protected override void SetupDependencies()
    {
        OnDependency<IPopupPresenter>()
            .Setup(x => x.CatchPossibleExceptionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(action => action());

        OnDependency<FohjinApiClient>()
            .Setup(x => x.CreateClientAsync(It.IsAny<CreateClientRequest>()))
            .Callback<CreateClientRequest>(x => CreateClientCommand = x)
            .Returns(Task.CompletedTask);
    }

    protected override void Given()
    {
        Presenter?.SetClient(null);
        Presenter?.Display();
        On<IClientDetailsView>().ValueFor(x => x.ClientName).IsSetTo("New Client Name");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
        On<IClientDetailsView>().FireEvent(x => x.OnSaveNewClientName += null);

        On<IClientDetailsView>().ValueFor(x => x.Street).IsSetTo("Street");
        On<IClientDetailsView>().ValueFor(x => x.StreetNumber).IsSetTo("123");
        On<IClientDetailsView>().ValueFor(x => x.PostalCode).IsSetTo("5000");
        On<IClientDetailsView>().ValueFor(x => x.City).IsSetTo("Bergen");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
        On<IClientDetailsView>().FireEvent(x => x.OnSaveNewAddress += null);

        On<IClientDetailsView>().ValueFor(x => x.PhoneNumber).IsSetTo("1234567890");
        On<IClientDetailsView>().FireEvent(x => x.OnFormElementGotChanged += null);
    }

    protected override void When()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnSaveNewPhoneNumber += null);
    }

    [TestMethod]
    public void Then_the_save_button_will_be_disabled()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.DisableSaveButton()).WasCalled();
    }

    [TestMethod]
    public void Then_a_create_client_command_with_all_collected_information_will_be_published()
    {
        On<FohjinApiClient>().VerifyThat.Method(x => x.CreateClientAsync(It.IsAny<CreateClientRequest>())).WasCalled();

        CreateClientCommand.ClientName.WillBe("New Client Name");
        CreateClientCommand.Street.WillBe("Street");
        CreateClientCommand.StreetNumber.WillBe("123");
        CreateClientCommand.PostalCode.WillBe("5000");
        CreateClientCommand.City.WillBe("Bergen");
        CreateClientCommand.PhoneNumber.WillBe("1234567890");
    }

    [TestMethod]
    public void Then_overview_panel_will_be_shown()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.Close()).WasCalled();
    }
}