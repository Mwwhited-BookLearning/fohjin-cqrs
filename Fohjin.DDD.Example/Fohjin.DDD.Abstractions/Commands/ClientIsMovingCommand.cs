namespace Fohjin.DDD.Commands;

public record ClientIsMovingCommand(
    Guid Id,
    string? Street,
    string? StreetNumber,
    string? PostalCode,
    string? City
    ) : CommandBase(Id);
