using Fohjin.DDD.EventStore.SQLite;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.BankApplication;

public class DomainDatabaseBootStrapper
{
    public const string DataBaseFile = "domainDataBase.db3";

    public async Task ReCreateDatabaseSchema(string dataBaseFile)
    {
        await using var context = CreateContext(dataBaseFile);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task CreateDatabaseSchemaIfNeeded(string dataBaseFile)
    {
        await using var context = CreateContext(dataBaseFile);
        await context.Database.MigrateAsync();
    }

    private static DomainEventStoreDbContext CreateContext(string dataBaseFile)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DomainEventStoreDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dataBaseFile}");
        return new DomainEventStoreDbContext(optionsBuilder.Options);
    }
}
