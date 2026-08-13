using Fohjin.DDD.Reporting.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.Reporting;

public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<ClientReport> ClientReports => Set<ClientReport>();
    public DbSet<ClientDetailsReport> ClientDetailsReports => Set<ClientDetailsReport>();
    public DbSet<AccountReport> AccountReports => Set<AccountReport>();
    public DbSet<AccountDetailsReport> AccountDetailsReports => Set<AccountDetailsReport>();
    public DbSet<LedgerReport> LedgerReports => Set<LedgerReport>();
    public DbSet<BankCardReport> BankCardReports => Set<BankCardReport>();

    // ClientDetailsReport.AllAccounts/BankCards and AccountDetailsReport.Ledgers are real EF
    // navigations (docs/08-reporting-read-models.md) - IReportingRepository.Query<TDto>()/
    // GetByIdAsync<TDto>() compose against them normally (.Include(), OData $expand, nested
    // OData routes). ClientDetailsReport.Accounts/ClosedAccounts stay computed, .Ignore()d
    // filtered views over AllAccounts by Status, not separate navigations.
    // AccountReport/AccountDetailsReport carry their own Status ("Open"/"Closed") rather than
    // being split into a second ClosedAccountReport/ClosedAccountDetailsReport type/table - that
    // split used to force LedgerReport.AccountDetailsReportId to mean "whichever of two tables
    // currently has this id", which a single FK property can't represent; one table with a
    // status column has one unambiguous target instead.
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
            entity.HasMany(x => x.AllAccounts).WithOne().HasForeignKey(x => x.ClientDetailsReportId);
            entity.HasMany(x => x.BankCards).WithOne().HasForeignKey(x => x.ClientDetailsReportId);
            entity.Ignore(x => x.Accounts);
            entity.Ignore(x => x.ClosedAccounts);
        });

        modelBuilder.Entity<AccountReport>(entity =>
        {
            entity.ToTable(nameof(AccountReport));
            entity.HasKey(x => x.Id);
            entity.Property<long>(InsertionSequenceShadowProperty).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<AccountDetailsReport>(entity =>
        {
            entity.ToTable(nameof(AccountDetailsReport));
            entity.HasKey(x => x.Id);
            entity.HasMany(x => x.Ledgers).WithOne().HasForeignKey(x => x.AccountDetailsReportId);
        });

        modelBuilder.Entity<LedgerReport>(entity =>
        {
            entity.ToTable(nameof(LedgerReport));
            entity.HasKey(x => x.Id);
            entity.Property<long>(InsertionSequenceShadowProperty).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<BankCardReport>(entity =>
        {
            entity.ToTable(nameof(BankCardReport));
            entity.HasKey(x => x.Id);
            entity.Property<long>(InsertionSequenceShadowProperty).ValueGeneratedOnAdd();
        });
    }

    // Reflection-loaded child collections (SqlServerReportingRepository.GetChildrenOfTypeAsync)
    // need a deterministic order - e.g. an account's Ledgers must read back in the order the
    // transactions happened. SQLite happened to return rows in insertion (rowid) order with no
    // ORDER BY; SQL Server does not make that guarantee. A shadow property (not a real CLR
    // property on the DTO) gives every "child" entity a DB-assigned insertion order without
    // adding a field to the public DTO/OpenAPI/NSwag-generated client contract.
    public const string InsertionSequenceShadowProperty = "InsertionSequence";
}
