namespace Fohjin.DDD.Commands;

public record WithdrawalCashCommand(Guid Id, decimal Amount) : CommandBase(Id);
