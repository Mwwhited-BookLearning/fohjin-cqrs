using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fohjin.DDD.EventStore.SQLite;

// Used only by `dotnet ef migrations` design-time tooling; the app resolves this
// context's options through DI (see ServiceCollectionExtensions) instead.
public class DomainEventStoreDbContextFactory : IDesignTimeDbContextFactory<DomainEventStoreDbContext>
{
    public DomainEventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DomainEventStoreDbContext>();
        optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable(DomainEventStorageConfig.ConnectionStringConfigKey)
            ?? "Server=127.0.0.1,14330;Database=FohjinDomainEventStore;User Id=sa;Password=Dev!Passw0rd;TrustServerCertificate=True;Encrypt=False");
        return new DomainEventStoreDbContext(optionsBuilder.Options);
    }
}
