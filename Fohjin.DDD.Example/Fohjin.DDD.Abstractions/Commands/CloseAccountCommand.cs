namespace Fohjin.DDD.Commands;

public record CloseAccountCommand(Guid Id) : CommandBase(Id);
