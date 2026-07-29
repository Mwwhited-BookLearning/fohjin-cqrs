namespace Fohjin.DDD.Commands;

public record MoneyTransferFailedCompensatingCommand(Guid Id, decimal Amount, string? AccountNumber) : CommandBase(Id);
