using System.Text.Json.Serialization;

namespace Fohjin.DDD.Reporting.Dtos;

public record AccountDetailsReport
{
    public Guid Id { get; init; }
    public Guid ClientReportId { get; init; }
    public List<LedgerReport> Ledgers { get; init; } = new();
    public string? AccountName { get; init; }
    public decimal Balance { get; init; }
    public string? AccountNumber { get; init; }

    // "Open" or "Closed" - replaces the former separate ClosedAccountDetailsReport type/table,
    // see AccountReport.Status for why.
    public string Status { get; init; } = "Open";

    [JsonConstructor]
    public AccountDetailsReport()
    {
    }

    public AccountDetailsReport(
        Guid id,
        Guid clientReportId,
        string? accountName,
        decimal balance,
        string? accountNumber,
        string status = "Open"
        )
    {
        Id = id;
        ClientReportId = clientReportId;
        Ledgers = [];
        AccountName = accountName;
        Balance = balance;
        AccountNumber = accountNumber;
        Status = status;
    }

    public static AccountDetailsReport New => new() { Id = Guid.NewGuid() };
}