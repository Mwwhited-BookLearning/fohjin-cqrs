using Fohjin.DDD.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fohjin.DDD.Reporting;

public static class ServiceCollectionExtensions
{
    // See Fohjin.DDD.EventStore.SqlServer.DomainEventStorageConfig for why this uses Aspire's
    // ConnectionStrings:<name> convention rather than a project-specific config section.
    public const string ConnectionStringName = "reportingdb";
    public const string ConnectionStringConfigKey = $"ConnectionStrings:{ConnectionStringName}";
    private const string DefaultConnectionString = "Server=127.0.0.1,14330;Database=FohjinReporting;User Id=sa;Password=Dev!Passw0rd;TrustServerCertificate=True;Encrypt=False";

    public static T AddReportingServices<T>(this T service) where T : IServiceCollection
    {
        service.AddDbContextFactory<ReportingDbContext>((sp, options) =>
            options.UseSqlServer(sp.GetService<IConfiguration>()?.GetConnectionString(ConnectionStringName) ?? DefaultConnectionString));

        service.TryAddTransient<IReportingRepository, SqlServerReportingRepository>();

        return service;
    }
}
