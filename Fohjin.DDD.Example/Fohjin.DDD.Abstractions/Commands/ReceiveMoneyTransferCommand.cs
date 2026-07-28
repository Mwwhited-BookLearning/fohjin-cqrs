namespace Fohjin.DDD.Commands;

public record ReceiveMoneyTransferCommand(Guid Id, decimal Amount, string? AccountNumber) : CommandBase(Id);
