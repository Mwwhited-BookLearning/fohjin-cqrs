using Fohjin.DDD.EventStore.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fohjin.DDD.Bootstrap;

public static class ServiceProviderExtensions
{
    public static async Task<T> BootStrapApplicationAsync<T>(this T serviceProvider) where T : IServiceProvider
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var domainConnectionString = configuration[DomainEventStorageConfig.ConnectionStringConfigKey]
            ?? throw new NotSupportedException($"configuration for {DomainEventStorageConfig.ConnectionStringConfigKey} is missing");
        var reportingConnectionString = configuration[Fohjin.DDD.Reporting.ServiceCollectionExtensions.ConnectionStringConfigKey]
            ?? throw new NotSupportedException($"configuration for {Fohjin.DDD.Reporting.ServiceCollectionExtensions.ConnectionStringConfigKey} is missing");

        await ActivatorUtilities.CreateInstance<DomainDatabaseBootStrapper>(serviceProvider)
            .CreateDatabaseSchemaIfNeeded(domainConnectionString);
        await ActivatorUtilities.CreateInstance<ReportingDatabaseBootStrapper>(serviceProvider)
            .CreateDatabaseSchemaIfNeeded(reportingConnectionString);

        return serviceProvider;
    }
}
