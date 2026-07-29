using Fohjin.DDD.ApiClient;

namespace Fohjin.DDD.ApiClient.Tests;

// Phase 6 exit criteria: the Client Details edit commands (name, address, phone number, open
// account) all reach the domain through the generated client and show up in ClientDetailsReport.
[TestClass]
[TestCategory("integration")]
public class ClientDetailsAndEditCommandsTest : WebApiIntegrationTestFixture
{
    private FohjinApiClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _client = new FohjinApiClient(HttpClient) { BaseUrl = HttpClient.BaseAddress!.ToString() };
    }

    [TestMethod]
    public async Task Generated_client_can_edit_client_details_and_open_an_account()
    {
        await _client.CreateClientAsync(new CreateClientRequest
        {
            ClientName = "Details Test Client",
            Street = "Welhavens gate",
            StreetNumber = "49b",
            PostalCode = "5006",
            City = "Bergen",
            PhoneNumber = "95009937",
        });

        var created = await PollUntilFoundAsync(() => _client.GetClientsAsync(), c => c.Name == "Details Test Client");
        Assert.IsNotNull(created, "the created client did not appear in GetClientsAsync within the retry window");

        var details = await _client.GetClientDetailsByIdAsync(created!.Id);
        Assert.AreEqual("Details Test Client", details.ClientName);
        Assert.AreEqual("Bergen", details.City);

        await _client.ChangeClientNameAsync(created.Id, new ChangeClientNameRequest { ClientName = "Renamed Client" });
        await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.ClientName == "Renamed Client");

        await _client.ChangeClientAddressAsync(created.Id, new ClientIsMovingRequest
        {
            Street = "New Street",
            StreetNumber = "1",
            PostalCode = "1000",
            City = "Oslo",
        });
        await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.City == "Oslo");

        await _client.ChangeClientPhoneNumberAsync(created.Id, new ChangeClientPhoneNumberRequest { PhoneNumber = "12345678" });
        await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.PhoneNumber == "12345678");

        await _client.OpenNewAccountForClientAsync(created.Id, new OpenNewAccountForClientRequest { AccountName = "Savings" });
        var withAccount = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.Accounts.Any(a => a.AccountName == "Savings"));

        Assert.AreEqual("Renamed Client", withAccount.ClientName);
        Assert.AreEqual("New Street", withAccount.Street);
        Assert.AreEqual("Oslo", withAccount.City);
        Assert.AreEqual("12345678", withAccount.PhoneNumber);
        Assert.IsTrue(withAccount.Accounts.Any(a => a.AccountName == "Savings"));
    }

    [TestMethod]
    public async Task GetClientDetailsById_returns_404_for_unknown_id()
    {
        var exception = await Assert.ThrowsExactlyAsync<ApiException>(() => _client.GetClientDetailsByIdAsync(Guid.NewGuid()));
        Assert.AreEqual(404, exception.StatusCode);
    }

    // DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so reads made
    // right after a command/create returns need to poll briefly rather than assume the read
    // model has caught up already - same pattern as CreateAndReadClientThroughGeneratedApiClientTest.
    private static async Task<T?> PollUntilFoundAsync<T>(Func<Task<ICollection<T>>> fetch, Func<T, bool> predicate) where T : class
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var match = (await fetch()).FirstOrDefault(predicate);
            if (match is not null)
                return match;

            await Task.Delay(100);
        }

        return null;
    }

    private static async Task<T> PollUntilAsync<T>(Func<Task<T>> fetch, Func<T, bool> predicate)
    {
        T result = await fetch();
        for (var attempt = 0; attempt < 30 && !predicate(result); attempt++)
        {
            await Task.Delay(100);
            result = await fetch();
        }

        Assert.IsTrue(predicate(result), "condition was not met within the retry window");
        return result;
    }
}
