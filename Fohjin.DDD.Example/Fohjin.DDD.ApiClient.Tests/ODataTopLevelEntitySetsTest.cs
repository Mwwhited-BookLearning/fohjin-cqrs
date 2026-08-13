using System.Net;
using System.Net.Http.Json;

namespace Fohjin.DDD.ApiClient.Tests;

// Task #103 generalized the /odata/Clients-only hand-rolled OData handler into
// EndpointRouteBuilderExtensions.MapODataEntitySet<TDto>, called once per top-level reporting
// DTO (Clients/ClientDetails/Accounts/AccountDetails/Ledgers/BankCards). ODataClientsEndpointTest.cs
// already pins that code path in detail for Clients; this only needs to prove the same generic
// path actually works end to end for a *different* DTO too, not repeat every case per entity set.
[TestClass]
[TestCategory("integration")]
public class ODataTopLevelEntitySetsTest : WebApiIntegrationTestFixture
{
    private static readonly HttpMethod Query = new("QUERY");
    private record AccountRow(Guid Id, Guid ClientDetailsReportId, string? AccountName, string? AccountNumber, string? Status);
    private record ClientRow(Guid Id, string? Name);

    private async Task<Guid> CreateClientWithAccountAsync(string clientName, string accountName)
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

        Guid clientId = Guid.Empty;
        for (var attempt = 0; attempt < 30 && clientId == Guid.Empty; attempt++)
        {
            var clients = await HttpClient.GetFromJsonAsync<List<ClientRow>>("/api/clients") ?? [];
            clientId = clients.FirstOrDefault(c => c.Name == clientName)?.Id ?? Guid.Empty;
            if (clientId == Guid.Empty)
                await Task.Delay(100);
        }
        Assert.AreNotEqual(Guid.Empty, clientId, $"client '{clientName}' never appeared");

        await HttpClient.PostAsJsonAsync($"/api/clients/{clientId}/accounts", new { accountName });

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var accounts = await HttpClient.GetFromJsonAsync<List<AccountRow>>("/odata/Accounts") ?? [];
            var match = accounts.FirstOrDefault(a => a.AccountName == accountName);
            if (match is not null)
                return match.Id;
            await Task.Delay(100);
        }

        Assert.Fail($"account '{accountName}' never appeared in /odata/Accounts");
        return Guid.Empty;
    }

    [TestMethod]
    public async Task GET_filter_and_QUERY_body_filter_return_equivalent_results_for_Accounts()
    {
        var accountId = await CreateClientWithAccountAsync("OData Accounts Client", "Rare Account Name Xyz");

        var getResult = await HttpClient.GetFromJsonAsync<List<AccountRow>>("/odata/Accounts?$filter=AccountName eq 'Rare Account Name Xyz'");

        var queryRequest = new HttpRequestMessage(Query, "/odata/Accounts") { Content = JsonContent.Create(new { filter = "AccountName eq 'Rare Account Name Xyz'" }) };
        var queryResponse = await HttpClient.SendAsync(queryRequest);
        var queryResult = await queryResponse.Content.ReadFromJsonAsync<List<AccountRow>>();

        Assert.IsNotNull(getResult);
        Assert.IsNotNull(queryResult);
        Assert.AreEqual(1, getResult!.Count);
        CollectionAssert.AreEquivalent(getResult.Select(a => a.Id).ToList(), queryResult!.Select(a => a.Id).ToList());
        Assert.AreEqual(accountId, getResult.Single().Id);
    }

    [TestMethod]
    public async Task GET_select_is_rejected_for_every_top_level_entity_set()
    {
        foreach (var entitySet in new[] { "Clients", "ClientDetails", "Accounts", "AccountDetails", "Ledgers", "BankCards" })
        {
            var response = await HttpClient.GetAsync($"/odata/{entitySet}?$select=Id");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"expected /odata/{entitySet}?$select=Id to be rejected");
        }
    }

    [TestMethod]
    public async Task Every_top_level_entity_set_responds_with_200_and_a_bare_JSON_array_when_empty()
    {
        foreach (var entitySet in new[] { "Clients", "ClientDetails", "Accounts", "AccountDetails", "Ledgers", "BankCards" })
        {
            var response = await HttpClient.GetAsync($"/odata/{entitySet}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"expected /odata/{entitySet} to respond 200");

            var body = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(body.TrimStart().StartsWith('['), $"expected /odata/{entitySet} to return a bare JSON array, not an OData envelope, got: {body}");
        }
    }
}
