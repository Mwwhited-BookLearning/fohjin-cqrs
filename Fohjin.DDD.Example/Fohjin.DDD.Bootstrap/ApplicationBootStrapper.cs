using Fohjin.DDD.EventStore.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fohjin.DDD.Bootstrap;

public static class ServiceProviderExtensions
{
    public static async Task<T> BootStrapApplicationAsync<T>(this T serviceProvider) where T : IServiceProvider
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var domainConnectionString = configuration.GetConnectionString(DomainEventStorageConfig.ConnectionStringName)
            ?? throw new NotSupportedException($"connection string '{DomainEventStorageConfig.ConnectionStringName}' is missing");
        var reportingConnectionString = configuration.GetConnectionString(Fohjin.DDD.Reporting.ServiceCollectionExtensions.ConnectionStringName)
            ?? throw new NotSupportedException($"connection string '{Fohjin.DDD.Reporting.ServiceCollectionExtensions.ConnectionStringName}' is missing");

        await ActivatorUtilities.CreateInstance<DomainDatabaseBootStrapper>(serviceProvider)
            .CreateDatabaseSchemaIfNeeded(domainConnectionString);
        await ActivatorUtilities.CreateInstance<ReportingDatabaseBootStrapper>(serviceProvider)
            .CreateDatabaseSchemaIfNeeded(reportingConnectionString);

        return serviceProvider;
    }
}
