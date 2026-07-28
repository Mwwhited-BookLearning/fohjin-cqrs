using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fohjin.DDD.EventStore.SQLite
{
    // Used only by `dotnet ef migrations` design-time tooling; the app resolves this
    // context's options through DI (see ServiceCollectionExtensions) instead.
    public class DomainEventStoreDbContextFactory : IDesignTimeDbContextFactory<DomainEventStoreDbContext>
    {
        public DomainEventStoreDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DomainEventStoreDbContext>();
            optionsBuilder.UseSqlite(Environment.GetEnvironmentVariable(DomainEventStorageConfig.ConnectionStringConfigKey) ?? "Data Source=domainDataBase.db3");
            return new DomainEventStoreDbContext(optionsBuilder.Options);
        }
    }
}
