namespace Fohjin.DDD.Commands;

public record CancelBankCardCommand(Guid Id, Guid BankCardId) : CommandBase(Id);
