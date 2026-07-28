namespace Fohjin.DDD.Commands;

public record ChangeClientNameCommand(Guid Id, string? ClientName) : CommandBase(Id);
