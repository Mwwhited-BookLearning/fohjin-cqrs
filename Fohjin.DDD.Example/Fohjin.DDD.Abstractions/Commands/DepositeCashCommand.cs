namespace Fohjin.DDD.Commands;

public record DepositCashCommand(Guid Id, decimal Amount) : CommandBase(Id);
