namespace Fohjin.DDD.Tests.TestUtilities;

// Local dev/CI SQL Server instance every test project connects to (docs/11-migration-plan.md
// Phase 8 - one DB engine for every environment, in place of the old per-test SQLite file).
// Isolation between tests now comes from each test getting its own database name on this
// shared server rather than its own .db3 file - see TestContextExtensions.GetDatabaseNameForTest.
public static class TestSqlServer
{
    public static string ConnectionStringFor(string databaseName) =>
        $"Server=127.0.0.1,14330;Database={databaseName};User Id=sa;Password=Dev!Passw0rd;TrustServerCertificate=True;Encrypt=False";
}
