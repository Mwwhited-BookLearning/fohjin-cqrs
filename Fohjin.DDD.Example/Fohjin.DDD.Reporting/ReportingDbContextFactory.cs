using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fohjin.DDD.Reporting;

// Used only by `dotnet ef migrations` design-time tooling; the app resolves this
// context's options through DI (see ServiceCollectionExtensions) instead.
public class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();
        optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable(ServiceCollectionExtensions.ConnectionStringConfigKey)
            ?? "Server=127.0.0.1,14330;Database=FohjinReporting;User Id=sa;Password=Dev!Passw0rd;TrustServerCertificate=True;Encrypt=False");
        return new ReportingDbContext(optionsBuilder.Options);
    }
}
