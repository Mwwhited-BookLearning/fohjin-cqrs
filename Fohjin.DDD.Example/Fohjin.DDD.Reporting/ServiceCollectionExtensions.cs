using Fohjin.DDD.Reporting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fohjin.DDD.Reporting;

public static class ServiceCollectionExtensions
{
    public const string ConnectionStringConfigKey = "Reporting:SqliteConnectionString";
    private const string DefaultSqLiteConnectionString = "Data Source=reportingDataBase.db3";

    public static T AddReportingServices<T>(this T service) where T : IServiceCollection
    {
        service.AddDbContextFactory<ReportingDbContext>((sp, options) =>
            options.UseSqlite(sp.GetService<IConfiguration>()?[ConnectionStringConfigKey] ?? DefaultSqLiteConnectionString));

        service.TryAddTransient<IReportingRepository, SqliteReportingRepository>();

        return service;
    }
}
