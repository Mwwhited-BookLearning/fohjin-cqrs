namespace Fohjin.DDD.Commands;

public record CreateClientCommand(
    Guid Id,
    string? ClientName,
    string? Street,
    string? StreetNumber,
    string? PostalCode,
    string? City,
    string? PhoneNumber
    ) : CommandBase(Id);
