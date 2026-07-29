using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Fohjin.DDD.ApiClient;

// Shared setup for tests that host Fohjin.DDD.WebApi in-memory via WebApplicationFactory<Program>.
//
// Isolation: each test run gets its own domain-event/reporting SQL Server database (created by
// ApplicationBootStrapper's Database.MigrateAsync() at host startup, dropped in BaseTearDown),
// via a ConfigureAppConfiguration override of DomainEventStorage:ConnectionString/
// Reporting:ConnectionString - the CWD-swap dance this fixture used under SQLite (each test's own
// process directory implied its own .db3 files) no longer applies now that isolation is a
// database name rather than a file path, so [assembly: Parallelize] would be safe to add here too
// if these tests turn out slow enough to warrant it.
[TestClass]
public abstract class WebApiIntegrationTestFixture
{
    private string _domainConnectionString = null!;
    private string _reportingConnectionString = null!;

    public TestContext TestContext { get; set; } = null!;

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient HttpClient { get; private set; } = null!;

    [TestInitialize]
    public void BaseSetup()
    {
        var runId = Guid.NewGuid().ToString("N");
        _domainConnectionString = ConnectionStringFor($"ApiClientTests_EventStore_{runId}");
        _reportingConnectionString = ConnectionStringFor($"ApiClientTests_Reporting_{runId}");

        // Phase 5 requires authorization on the command/query/SSE endpoints; these tests exercise
        // that endpoint behavior, not authentication itself, so the real JwtBearer scheme is
        // replaced with an always-succeeds test scheme (see TestAuthHandler) rather than standing
        // up a real dev STS per test run.
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eventstoredb"] = _domainConnectionString,
                ["ConnectionStrings:reportingdb"] = _reportingConnectionString,
            }));
            builder.ConfigureTestServices(services => services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
        });
        HttpClient = Factory.CreateClient();
    }

    [TestCleanup]
    public void BaseTearDown()
    {
        HttpClient.Dispose();
        Factory.Dispose();
        DropDatabase(_domainConnectionString);
        DropDatabase(_reportingConnectionString);
    }

    // Same local dev/CI SQL Server instance every project's tests connect to (docs/11-migration-plan.md Phase 8).
    private static string ConnectionStringFor(string databaseName) =>
        $"Server=127.0.0.1,14330;Database={databaseName};User Id=sa;Password=Dev!Passw0rd;TrustServerCertificate=True;Encrypt=False";

    private static void DropDatabase(string connectionString)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = connectionStringBuilder.InitialCatalog;
        connectionStringBuilder.InitialCatalog = "master";

        using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        var quotedName = new SqlCommandBuilder().QuoteIdentifier(databaseName);
        command.CommandText = $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE {quotedName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {quotedName}; END";
        command.ExecuteNonQuery();
    }
}
