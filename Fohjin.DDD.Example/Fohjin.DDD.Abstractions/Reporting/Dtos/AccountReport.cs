using System.Text.Json.Serialization;

namespace Fohjin.DDD.Reporting.Dtos;

public record AccountReport
{
    public Guid Id { get; init; }
    public Guid ClientDetailsReportId { get; init; }
    public string? AccountName { get; init; }
    public string? AccountNumber { get; init; }

    // "Open" or "Closed" - replaces the former separate ClosedAccountReport type/table.
    // The underlying event store keeps the actual immutable history; this read model only
    // ever needs to reflect the account's current state, not preserve a frozen snapshot of
    // it under a second identity (see docs/08-reporting-read-models.md).
    public string Status { get; init; } = "Open";

    [JsonConstructor]
    public AccountReport() { }

    public AccountReport(
        Guid id,
        Guid clientDetailsReportId,
        string? accountName,
        string? accountNumber,
        string status = "Open"
        )
    {
        Id = id;
        ClientDetailsReportId = clientDetailsReportId;
        AccountName = accountName;
        AccountNumber = accountNumber;
        Status = status;
    }

    public override string ToString() => $"{AccountNumber} - ({AccountName})";
}