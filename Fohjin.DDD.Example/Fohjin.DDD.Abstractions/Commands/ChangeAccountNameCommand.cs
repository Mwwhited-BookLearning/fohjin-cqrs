namespace Fohjin.DDD.Commands;

public record ChangeAccountNameCommand(Guid Id, string? AccountName) : CommandBase(Id);
