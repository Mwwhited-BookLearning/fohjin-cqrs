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

    [JsonConstructor]
    public AccountDetailsReport()
    {
    }

    public AccountDetailsReport(
        Guid id,
        Guid clientReportId,
        string? accountName,
        decimal balance,
        string? accountNumber
        )
    {
        Id = id;
        ClientReportId = clientReportId;
        Ledgers = [];
        AccountName = accountName;
        Balance = balance;
        AccountNumber = accountNumber;
    }

    public static AccountDetailsReport New => new() { Id = Guid.NewGuid() };
}