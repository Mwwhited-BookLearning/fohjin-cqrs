namespace Fohjin.DDD.Commands;

public record SendMoneyTransferCommand(Guid Id, decimal Amount, string? AccountNumber) : CommandBase(Id);
