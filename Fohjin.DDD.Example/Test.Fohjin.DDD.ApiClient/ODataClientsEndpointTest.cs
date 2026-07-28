using System.Net;
using System.Net.Http.Json;

namespace Test.Fohjin.DDD.ApiClient;

// Phase 3 exit criteria: GET /odata/Clients?$filter=... and QUERY /odata/Clients (filter in the
// body) return equivalent results for the same filter. Also covers two bugs found while manually
// verifying that live (both fixed in Program.cs, not just described here): $select used to throw
// a 500 instead of being rejected, and a bodyless QUERY request used to throw trying to parse a
// non-existent JSON body instead of behaving like "no filter".
[TestClass]
[TestCategory("integration")]
public class ODataClientsEndpointTest : WebApiIntegrationTestFixture
{
    private static readonly HttpMethod Query = new("QUERY");

    private async Task<List<ClientRow>> CreateTwoClientsAsync()
    {
        await HttpClient.PostAsJsonAsync("/api/clients", new
        {
            clientName = "Mark Nijhof",
            street = "Welhavens gate",
            streetNumber = "49b",
            postalCode = "5006",
            city = "Bergen",
            phoneNumber = "95009937",
        });
        await HttpClient.PostAsJsonAsync("/api/clients", new
        {
            clientName = "Jane Doe",
            street = "Main St",
            streetNumber = "1",
            postalCode = "1000",
            city = "Oslo",
            phoneNumber = "12345678",
        });

        // DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md) - poll rather
        // than assume both clients are already in the read model.
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var clients = await HttpClient.GetFromJsonAsync<List<ClientRow>>("/api/clients") ?? [];
            if (clients.Count >= 2)
                return clients;
            await Task.Delay(100);
        }

        Assert.Fail("the two created clients did not appear in GetClients within the retry window");
        return [];
    }

    [TestMethod]
    public async Task GET_filter_and_QUERY_body_filter_return_equivalent_results()
    {
        await CreateTwoClientsAsync();

        var getResult = await HttpClient.GetFromJsonAsync<List<ClientRow>>("/odata/Clients?$filter=Name eq 'Mark Nijhof'");

        var queryRequest = new HttpRequestMessage(Query, "/odata/Clients") { Content = JsonContent.Create(new { filter = "Name eq 'Mark Nijhof'" }) };
        var queryResponse = await HttpClient.SendAsync(queryRequest);
        var queryResult = await queryResponse.Content.ReadFromJsonAsync<List<ClientRow>>();

        Assert.IsNotNull(getResult);
        Assert.IsNotNull(queryResult);
        CollectionAssert.AreEquivalent(getResult!.Select(c => c.Id).ToList(), queryResult!.Select(c => c.Id).ToList());
        Assert.IsTrue(getResult.All(c => c.Name == "Mark Nijhof"));
    }

    [TestMethod]
    public async Task QUERY_with_no_body_behaves_like_no_filter()
    {
        var created = await CreateTwoClientsAsync();

        var queryResponse = await HttpClient.SendAsync(new HttpRequestMessage(Query, "/odata/Clients"));
        var queryResult = await queryResponse.Content.ReadFromJsonAsync<List<ClientRow>>();

        Assert.AreEqual(HttpStatusCode.OK, queryResponse.StatusCode);
        Assert.AreEqual(created.Count, queryResult?.Count);
    }

    [TestMethod]
    public async Task GET_select_is_rejected_rather_than_silently_wrong()
    {
        var response = await HttpClient.GetAsync("/odata/Clients?$select=Name");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record ClientRow(Guid Id, string? Name);
}
