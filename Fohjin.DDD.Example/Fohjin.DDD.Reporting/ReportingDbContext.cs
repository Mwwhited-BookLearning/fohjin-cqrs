using Fohjin.DDD.Reporting.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.Reporting;

public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<ClientReport> ClientReports => Set<ClientReport>();
    public DbSet<ClientDetailsReport> ClientDetailsReports => Set<ClientDetailsReport>();
    public DbSet<AccountReport> AccountReports => Set<AccountReport>();
    public DbSet<AccountDetailsReport> AccountDetailsReports => Set<AccountDetailsReport>();
    public DbSet<ClosedAccountReport> ClosedAccountReports => Set<ClosedAccountReport>();
    public DbSet<ClosedAccountDetailsReport> ClosedAccountDetailsReports => Set<ClosedAccountDetailsReport>();
    public DbSet<LedgerReport> LedgerReports => Set<LedgerReport>();

    // Child collections (Accounts/ClosedAccounts/Ledgers) are populated by the repository with
    // a follow-up query keyed on the existing "{ParentTypeName}Id" convention instead of being
    // modeled as EF navigations: LedgerReport.AccountDetailsReportId is shared, by that same
    // convention, between both AccountDetailsReport and ClosedAccountDetailsReport (a closed
    // account keeps its original account's id), which one FK property can't represent as two
    // distinct EF relationships at once.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientReport>(entity =>
        {
            entity.ToTable(nameof(ClientReport));
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ClientDetailsReport>(entity =>
        {
            entity.ToTable(nameof(ClientDetailsReport));
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Accounts);
            entity.Ignore(x => x.ClosedAccounts);
        });

        modelBuilder.Entity<AccountReport>(entity =>
        {
            entity.ToTable(nameof(AccountReport));
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<AccountDetailsReport>(entity =>
        {
            entity.ToTable(nameof(AccountDetailsReport));
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Ledgers);
        });

        modelBuilder.Entity<ClosedAccountReport>(entity =>
        {
            entity.HasBaseType((Type?)null);
            entity.ToTable(nameof(ClosedAccountReport));
            entity.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ClosedAccountDetailsReport>(entity =>
        {
            entity.HasBaseType((Type?)null);
            entity.ToTable(nameof(ClosedAccountDetailsReport));
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Ledgers);
        });

        modelBuilder.Entity<LedgerReport>(entity =>
        {
            entity.ToTable(nameof(LedgerReport));
            entity.HasKey(x => x.Id);
        });
    }
}
