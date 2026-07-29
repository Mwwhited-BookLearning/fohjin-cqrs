using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Presenters;
using Fohjin.DDD.BankApplication.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test.Fohjin.DDD.Scenarios.Adding_a_new_client;

[TestClass]
[TestCategory("unit")]
public class When_in_the_GUI_canceling_to_add_the_new_client : PresenterTestFixture<ClientDetailsPresenter>
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
        Presenter.SetClient(null);
        Presenter.Display();
        On<IClientDetailsView>().ValueFor(x => x.ClientName).IsSetTo("Client name");
        On<IClientDetailsView>().FireEvent(x => x.OnInitiateClientNameChange += null);
    }

    protected override void When()
    {
        On<IClientDetailsView>().FireEvent(x => x.OnCancel += null);
    }

    [TestMethod]
    public void Then_the_view_will_be_closed()
    {
        On<IClientDetailsView>().VerifyThat.Method(x => x.Close()).WasCalled();
    }
}