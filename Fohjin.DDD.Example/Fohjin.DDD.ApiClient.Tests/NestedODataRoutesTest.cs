using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Fohjin.DDD.ApiClient.Tests;

// Task #103's other half of "getbyx -> IQueryable/OData": nested/contained OData routes under a
// parent entity (the user's own example: /odata/ClientDetails(id)/Accounts?$filter=...), backed
// by real ODataController + [EnableQuery] rather than the hand-rolled minimal-API pattern
// ODataClientsEndpointTest.cs pins for the top-level entity sets - nothing before this test
// exercised OData routing conventions actually resolving these controllers at all.
[TestClass]
[TestCategory("integration")]
public class NestedODataRoutesTest : WebApiIntegrationTestFixture
{
    private record ClientRow(Guid Id, string? Name);
    private record AccountRow(Guid Id, Guid ClientDetailsReportId, string? AccountName, string? AccountNumber, string? Status);
    private record BankCardRow(Guid Id, Guid ClientDetailsReportId, Guid AccountId, string? Status);
    private record LedgerRow(Guid Id, Guid AccountDetailsReportId, string? Action, decimal Amount);

    // [EnableQuery]'s OData-JSON formatter wraps every collection result in an OData envelope
    // ({"@odata.context": "...", "value": [...]}), unlike the bare-JSON-array shape
    // MapODataEntitySet<TDto>'s hand-rolled top-level entity sets return (ODataClientsEndpointTest.cs
    // pins that difference) - callers of these nested routes unwrap "value" instead.
    private record ODataCollection<T>([property: JsonPropertyName("value")] List<T> Value);

    private async Task<List<T>> GetODataNestedCollectionAsync<T>(string url)
    {
        var result = await HttpClient.GetFromJsonAsync<ODataCollection<T>>(url);
        return result?.Value ?? [];
    }

    private async Task<Guid> CreateClientAsync(string clientName)
    {
        await HttpClient.PostAsJsonAsync("/api/clients", new
        {
            clientName,
            street = "Welhavens gate",
            streetNumber = "49b",
            postalCode = "5006",
            city = "Bergen",
            phoneNumber = "95009937",
        });

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var clients = await HttpClient.GetFromJsonAsync<List<ClientRow>>("/api/clients") ?? [];
            var match = clients.FirstOrDefault(c => c.Name == clientName);
            if (match is not null)
                return match.Id;
            await Task.Delay(100);
        }

        Assert.Fail($"the created client '{clientName}' did not appear in GetClients within the retry window");
        return Guid.Empty;
    }

    private async Task<Guid> OpenAccountAsync(Guid clientId, string accountName)
    {
        await HttpClient.PostAsJsonAsync($"/api/clients/{clientId}/accounts", new { accountName });

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var accounts = await GetODataNestedCollectionAsync<AccountRow>($"/odata/ClientDetails({clientId})/Accounts");
            var match = accounts.FirstOrDefault(a => a.AccountName == accountName);
            if (match is not null)
                return match.Id;
            await Task.Delay(100);
        }

        Assert.Fail($"the opened account '{accountName}' did not appear under the client's nested Accounts route within the retry window");
        return Guid.Empty;
    }

    [TestMethod]
    public async Task Nested_ClientDetails_Accounts_route_returns_only_open_accounts_for_that_client()
    {
        var clientId = await CreateClientAsync("Nested Accounts Client");
        var otherClientId = await CreateClientAsync("Other Nested Accounts Client");

        var accountId = await OpenAccountAsync(clientId, "Checking");
        await OpenAccountAsync(otherClientId, "Unrelated Account");

        var accounts = await GetODataNestedCollectionAsync<AccountRow>($"/odata/ClientDetails({clientId})/Accounts");

        Assert.AreEqual(1, accounts.Count);
        Assert.AreEqual(accountId, accounts.Single().Id);
        Assert.AreEqual(clientId, accounts.Single().ClientDetailsReportId);
    }

    [TestMethod]
    public async Task Nested_ClientDetails_Accounts_route_honors_dollar_filter()
    {
        var clientId = await CreateClientAsync("Nested Filter Client");
        await OpenAccountAsync(clientId, "Checking");
        await OpenAccountAsync(clientId, "Savings");

        var accounts = await GetODataNestedCollectionAsync<AccountRow>($"/odata/ClientDetails({clientId})/Accounts?$filter=AccountName eq 'Savings'");

        Assert.AreEqual(1, accounts.Count);
        Assert.AreEqual("Savings", accounts.Single().AccountName);
    }

    [TestMethod]
    public async Task Nested_ClientDetails_ClosedAccounts_route_only_returns_closed_accounts()
    {
        var clientId = await CreateClientAsync("Nested Closed Accounts Client");
        var accountId = await OpenAccountAsync(clientId, "To Be Closed");

        await HttpClient.PostAsJsonAsync($"/api/accounts/{accountId}/close", new { });

        var closedAccounts = new List<AccountRow>();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            closedAccounts = await GetODataNestedCollectionAsync<AccountRow>($"/odata/ClientDetails({clientId})/ClosedAccounts");
            if (closedAccounts.Count > 0)
                break;
            await Task.Delay(100);
        }

        Assert.AreEqual(1, closedAccounts.Count);
        Assert.AreEqual(accountId, closedAccounts.Single().Id);
        Assert.AreEqual("Closed", closedAccounts.Single().Status);

        var openAccounts = await GetODataNestedCollectionAsync<AccountRow>($"/odata/ClientDetails({clientId})/Accounts");
        Assert.AreEqual(0, openAccounts.Count);
    }

    [TestMethod]
    public async Task Nested_ClientDetails_BankCards_route_returns_cards_for_that_client()
    {
        var clientId = await CreateClientAsync("Nested Bank Cards Client");
        var accountId = await OpenAccountAsync(clientId, "Card Account");

        await HttpClient.PostAsJsonAsync($"/api/clients/{clientId}/bank-cards", new { accountId });

        var bankCards = new List<BankCardRow>();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            bankCards = await GetODataNestedCollectionAsync<BankCardRow>($"/odata/ClientDetails({clientId})/BankCards");
            if (bankCards.Count > 0)
                break;
            await Task.Delay(100);
        }

        Assert.AreEqual(1, bankCards.Count);
        Assert.AreEqual(accountId, bankCards.Single().AccountId);
    }

    [TestMethod]
    public async Task Nested_AccountDetails_Ledgers_route_returns_ledger_entries_for_that_account()
    {
        var clientId = await CreateClientAsync("Nested Ledgers Client");
        var accountId = await OpenAccountAsync(clientId, "Ledger Account");

        await HttpClient.PostAsJsonAsync($"/api/accounts/{accountId}/deposit", new { amount = 100m });

        var ledgers = new List<LedgerRow>();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            ledgers = await GetODataNestedCollectionAsync<LedgerRow>($"/odata/AccountDetails({accountId})/Ledgers");
            if (ledgers.Count > 0)
                break;
            await Task.Delay(100);
        }

        Assert.AreEqual(1, ledgers.Count);
        Assert.AreEqual("Deposit", ledgers.Single().Action);
        Assert.AreEqual(100m, ledgers.Single().Amount);
        Assert.AreEqual(accountId, ledgers.Single().AccountDetailsReportId);
    }

    [TestMethod]
    public async Task Nested_route_for_unknown_parent_id_returns_an_empty_list_not_an_error()
    {
        var response = await HttpClient.GetAsync($"/odata/ClientDetails({Guid.NewGuid()})/Accounts");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<ODataCollection<AccountRow>>();
        Assert.IsNotNull(accounts);
        Assert.AreEqual(0, accounts!.Value.Count);
    }
}
