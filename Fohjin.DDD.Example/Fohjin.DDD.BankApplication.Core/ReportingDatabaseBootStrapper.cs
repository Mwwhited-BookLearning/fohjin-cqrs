using Fohjin.DDD.Reporting;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.BankApplication
{
    public class ReportingDatabaseBootStrapper
    {
        public const string ReportingDataBaseFile = "reportingDataBase.db3";

        public async Task ReCreateDatabaseSchema(string dataBaseFile)
        {
            await using var context = CreateContext(dataBaseFile);
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }

        public async Task CreateDatabaseSchemaIfNeeded(string dataBaseFile)
        {
            await using var context = CreateContext(dataBaseFile);
            await context.Database.MigrateAsync();
        }

        private static ReportingDbContext CreateContext(string dataBaseFile)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dataBaseFile}");
            return new ReportingDbContext(optionsBuilder.Options);
        }
    }
}
