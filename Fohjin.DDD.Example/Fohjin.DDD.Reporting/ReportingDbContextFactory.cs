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
        optionsBuilder.UseSqlite("Data Source=reportingDataBase.db3");
        return new ReportingDbContext(optionsBuilder.Options);
    }
}
