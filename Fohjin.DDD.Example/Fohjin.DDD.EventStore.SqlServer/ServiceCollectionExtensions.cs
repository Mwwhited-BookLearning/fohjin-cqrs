using Fohjin.DDD.EventStore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fohjin.DDD.EventStore.SqlServer;

public static class ServiceCollectionExtensions
{
    public static T AddEventStoreSqlServerServices<T>(this T service) where T : IServiceCollection
    {
        service.AddDbContextFactory<DomainEventStoreDbContext>((sp, options) =>
            options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString(DomainEventStorageConfig.ConnectionStringName)
                ?? throw new NotSupportedException($"connection string '{DomainEventStorageConfig.ConnectionStringName}' is missing")));

        service.TryAddSingleton(typeof(IDomainEventStorage<>), typeof(DomainEventStorage<>));
        return service;
    }
}
