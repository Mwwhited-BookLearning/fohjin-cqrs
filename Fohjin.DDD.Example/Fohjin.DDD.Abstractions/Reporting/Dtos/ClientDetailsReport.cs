using System.Text.Json.Serialization;

namespace Fohjin.DDD.Reporting.Dtos;

public record ClientDetailsReport
{
    public Guid Id { get; set; }

    // The real EF navigation (every account belonging to this client, open and closed alike) -
    // Accounts/ClosedAccounts below are what actually get serialized to callers, filtered
    // views over this same collection, matching the wire shape from before AccountReport and
    // ClosedAccountReport were merged into one type with a Status field.
    [JsonIgnore]
    public List<AccountReport> AllAccounts { get; set; } = new();

    public IEnumerable<AccountReport> Accounts => AllAccounts.Where(a => a.Status != "Closed");
    public IEnumerable<AccountReport> ClosedAccounts => AllAccounts.Where(a => a.Status == "Closed");

    public List<BankCardReport> BankCards { get; set; } = new();
    public string? ClientName { get; set; }
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }

    [JsonConstructor]
    public ClientDetailsReport()
    {
    }

    public ClientDetailsReport(
        Guid id,
        string? clientName,
        string? street,
        string? streetNumber,
        string? postalCode,
        string? city,
        string? phoneNumber
        )
    {
        Id = id;
        ClientName = clientName;
        Street = street;
        StreetNumber = streetNumber;
        PostalCode = postalCode;
        City = city;
        PhoneNumber = phoneNumber;
    }

    public static ClientDetailsReport New => new () { Id = Guid.NewGuid(), };
}