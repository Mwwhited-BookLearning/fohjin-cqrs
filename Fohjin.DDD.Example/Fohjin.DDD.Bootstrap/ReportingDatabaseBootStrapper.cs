using Fohjin.DDD.Reporting;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.Bootstrap;

public class ReportingDatabaseBootStrapper
{
    public async Task ReCreateDatabaseSchema(string connectionString)
    {
        await using var context = CreateContext(connectionString);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task CreateDatabaseSchemaIfNeeded(string connectionString)
    {
        await using var context = CreateContext(connectionString);
        await context.Database.MigrateAsync();
    }

    private static ReportingDbContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new ReportingDbContext(optionsBuilder.Options);
    }
}
