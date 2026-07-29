using System.Text.Json.Serialization;

namespace Fohjin.DDD.Reporting.Dtos;

public record BankCardReport
{
    public Guid Id { get; init; }
    public Guid ClientDetailsReportId { get; init; }
    public Guid AccountId { get; init; }
    public string? Status { get; init; }

    [JsonConstructor]
    public BankCardReport() { }

    public BankCardReport(
        Guid id,
        Guid clientDetailsReportId,
        Guid accountId,
        string? status
        )
    {
        Id = id;
        ClientDetailsReportId = clientDetailsReportId;
        AccountId = accountId;
        Status = status;
    }

    public override string ToString() => $"Bank card ({Status})";
}
