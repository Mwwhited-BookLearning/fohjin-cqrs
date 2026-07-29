using Fohjin.DDD.EventStore.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.Bootstrap;

public class DomainDatabaseBootStrapper
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

    private static DomainEventStoreDbContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DomainEventStoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new DomainEventStoreDbContext(optionsBuilder.Options);
    }
}
