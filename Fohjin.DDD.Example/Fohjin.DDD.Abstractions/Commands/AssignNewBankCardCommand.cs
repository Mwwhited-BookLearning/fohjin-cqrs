namespace Fohjin.DDD.Commands;

public record AssignNewBankCardCommand(Guid Id, Guid AccountId) : CommandBase(Id);
