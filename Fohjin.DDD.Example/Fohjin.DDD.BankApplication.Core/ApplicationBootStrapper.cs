using Fohjin.DDD.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fohjin.DDD.BankApplication
{
    public static class ServiceProviderExtensions
    {
        public static async Task<T> BootStrapApplicationAsync<T>(this T serviceProvider) where T : IServiceProvider
        {
            var dataBaseFile = Path.GetFullPath(DomainDatabaseBootStrapper.DataBaseFile);
            var reportingFile = Path.GetFullPath(ReportingDatabaseBootStrapper.ReportingDataBaseFile);

            await ActivatorUtilities.CreateInstance<DomainDatabaseBootStrapper>(serviceProvider)
                .CreateDatabaseSchemaIfNeeded(dataBaseFile);
            await ActivatorUtilities.CreateInstance<ReportingDatabaseBootStrapper>(serviceProvider)
                .CreateDatabaseSchemaIfNeeded(reportingFile);

            return serviceProvider;
        }
    }
}
