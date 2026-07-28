using Microsoft.AspNetCore.Mvc.Testing;

namespace Test.Fohjin.DDD.ApiClient;

// Shared setup for tests that host Fohjin.DDD.WebApi in-memory via WebApplicationFactory<Program>.
//
// ApplicationBootStrapper (Fohjin.DDD.BankApplication.Core) migrates the domain-event and
// reporting SQLite databases at Path.GetFullPath("domainDataBase.db3"/"reportingDataBase.db3")
// - i.e. relative to the process's current directory, ignoring IConfiguration entirely - while
// the runtime DbContext factories resolve their connection string through configuration. Those
// two happen to agree in normal (dev) usage because appsettings.json points at the same relative
// filenames, but it means a config-only override (e.g. via ConfigureAppConfiguration) would make
// the bootstrapper migrate one file while the app queries another. So isolation here works by
// switching the process's current directory to a fresh temp folder instead, which both paths
// resolve against consistently. That makes tests derived from this fixture unsafe to run in
// parallel with anything else that depends on the process CWD - hence no [assembly: Parallelize]
// in this project.
[TestClass]
public abstract class WebApiIntegrationTestFixture
{
    private string _originalDirectory = null!;
    private string _tempDirectory = null!;

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient HttpClient { get; private set; } = null!;

    [TestInitialize]
    public void BaseSetup()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"Fohjin.DDD.ApiClient.Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        Directory.SetCurrentDirectory(_tempDirectory);

        Factory = new WebApplicationFactory<Program>();
        HttpClient = Factory.CreateClient();
    }

    [TestCleanup]
    public void BaseTearDown()
    {
        HttpClient.Dispose();
        Factory.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); // else the temp dir's .db3 files stay locked
        Directory.SetCurrentDirectory(_originalDirectory);
        Directory.Delete(_tempDirectory, recursive: true);
    }
}
