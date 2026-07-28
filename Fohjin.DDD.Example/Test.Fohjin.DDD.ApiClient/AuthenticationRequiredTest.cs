using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Test.Fohjin.DDD.ApiClient;

// Phase 5 exit criteria (partial - see docs/11-migration-plan.md for the full live verification
// against a real dev STS, which this test doesn't attempt): unauthenticated requests to the
// command/query/SSE endpoints are rejected. Deliberately does NOT use
// WebApiIntegrationTestFixture - that fixture swaps in an always-succeeds test auth scheme so
// other tests can exercise endpoint behavior without authentication getting in the way, which
// would defeat the point of this test.
[TestClass]
[TestCategory("integration")]
public class AuthenticationRequiredTest
{
    private string _originalDirectory = null!;
    private string _tempDirectory = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _httpClient = null!;

    [TestInitialize]
    public void Setup()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"Fohjin.DDD.ApiClient.Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);

        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();
    }

    [TestCleanup]
    public void TearDown()
    {
        _httpClient.Dispose();
        _factory.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.SetCurrentDirectory(_originalDirectory);
        Directory.Delete(_tempDirectory, recursive: true);
    }

    [TestMethod]
    public async Task GET_clients_without_a_token_is_rejected()
    {
        var response = await _httpClient.GetAsync("/api/clients");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task POST_clients_without_a_token_is_rejected()
    {
        var response = await _httpClient.PostAsync("/api/clients", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GET_odata_clients_without_a_token_is_rejected()
    {
        var response = await _httpClient.GetAsync("/odata/Clients");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GET_events_stream_without_a_token_is_rejected()
    {
        var response = await _httpClient.GetAsync("/api/events");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
