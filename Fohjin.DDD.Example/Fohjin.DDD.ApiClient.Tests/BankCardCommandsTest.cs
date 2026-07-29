using Fohjin.DDD.ApiClient;

namespace Fohjin.DDD.ApiClient.Tests;

// Bank cards (docs/02-bank-cards.md): the domain/command layer existed from the original CQRS
// demo, but nothing ever called it until this test's endpoints/UI were added. Verifies the new
// AssignNewBankCard/CancelBankCard/ReportStolenBankCard endpoints and the BankCardReport read
// model end to end, through the generated client, the same way ClientDetailsAndEditCommandsTest
// verifies the pre-existing client/account endpoints.
[TestClass]
[TestCategory("integration")]
public class BankCardCommandsTest : WebApiIntegrationTestFixture
{
    private FohjinApiClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _client = new FohjinApiClient(HttpClient) { BaseUrl = HttpClient.BaseAddress!.ToString() };
    }

    [TestMethod]
    public async Task Assigning_a_bank_card_shows_up_as_Active_under_the_client()
    {
        await _client.CreateClientAsync(new CreateClientRequest
        {
            ClientName = "Bank Card Test Client",
            Street = "Welhavens gate",
            StreetNumber = "49b",
            PostalCode = "5006",
            City = "Bergen",
            PhoneNumber = "95009937",
        });
        var created = await PollUntilFoundAsync(() => _client.GetClientsAsync(), c => c.Name == "Bank Card Test Client");
        Assert.IsNotNull(created, "the created client did not appear in GetClientsAsync within the retry window");

        await _client.OpenNewAccountForClientAsync(created!.Id, new OpenNewAccountForClientRequest { AccountName = "Checking" });
        var withAccount = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.Accounts.Any(a => a.AccountName == "Checking"));
        var accountId = withAccount.Accounts.Single(a => a.AccountName == "Checking").Id;

        await _client.AssignNewBankCardAsync(created.Id, new AssignNewBankCardRequest { AccountId = accountId });
        var withCard = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.BankCards.Any());

        var card = withCard.BankCards.Single();
        Assert.AreEqual(accountId, card.AccountId);
        Assert.AreEqual("Active", card.Status);
    }

    [TestMethod]
    public async Task Canceling_a_bank_card_marks_it_Cancelled()
    {
        await _client.CreateClientAsync(new CreateClientRequest
        {
            ClientName = "Cancel Card Test Client",
            Street = "Welhavens gate",
            StreetNumber = "49b",
            PostalCode = "5006",
            City = "Bergen",
            PhoneNumber = "95009937",
        });
        var created = await PollUntilFoundAsync(() => _client.GetClientsAsync(), c => c.Name == "Cancel Card Test Client");
        Assert.IsNotNull(created);

        await _client.OpenNewAccountForClientAsync(created!.Id, new OpenNewAccountForClientRequest { AccountName = "Checking" });
        var withAccount = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.Accounts.Any());
        var accountId = withAccount.Accounts.Single().Id;

        await _client.AssignNewBankCardAsync(created.Id, new AssignNewBankCardRequest { AccountId = accountId });
        var withCard = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.BankCards.Any());
        var bankCardId = withCard.BankCards.Single().Id;

        await _client.CancelBankCardAsync(created.Id, bankCardId);
        var afterCancel = await PollUntilAsync(
            () => _client.GetClientDetailsByIdAsync(created.Id),
            d => d.BankCards.Single().Status == "Cancelled");

        Assert.AreEqual("Cancelled", afterCancel.BankCards.Single().Status);
    }

    [TestMethod]
    public async Task Reporting_a_bank_card_stolen_marks_it_ReportedStolen()
    {
        await _client.CreateClientAsync(new CreateClientRequest
        {
            ClientName = "Stolen Card Test Client",
            Street = "Welhavens gate",
            StreetNumber = "49b",
            PostalCode = "5006",
            City = "Bergen",
            PhoneNumber = "95009937",
        });
        var created = await PollUntilFoundAsync(() => _client.GetClientsAsync(), c => c.Name == "Stolen Card Test Client");
        Assert.IsNotNull(created);

        await _client.OpenNewAccountForClientAsync(created!.Id, new OpenNewAccountForClientRequest { AccountName = "Checking" });
        var withAccount = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.Accounts.Any());
        var accountId = withAccount.Accounts.Single().Id;

        await _client.AssignNewBankCardAsync(created.Id, new AssignNewBankCardRequest { AccountId = accountId });
        var withCard = await PollUntilAsync(() => _client.GetClientDetailsByIdAsync(created.Id), d => d.BankCards.Any());
        var bankCardId = withCard.BankCards.Single().Id;

        await _client.ReportStolenBankCardAsync(created.Id, bankCardId);
        var afterReport = await PollUntilAsync(
            () => _client.GetClientDetailsByIdAsync(created.Id),
            d => d.BankCards.Single().Status == "ReportedStolen");

        Assert.AreEqual("ReportedStolen", afterReport.BankCards.Single().Status);
    }

    // DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so reads made
    // right after a command/create returns need to poll briefly rather than assume the read
    // model has caught up already - same pattern as ClientDetailsAndEditCommandsTest.
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
