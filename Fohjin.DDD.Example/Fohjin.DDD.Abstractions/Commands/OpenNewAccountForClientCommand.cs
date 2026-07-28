namespace Fohjin.DDD.Commands;

public record OpenNewAccountForClientCommand(Guid Id, string? AccountName) : CommandBase(Id);
