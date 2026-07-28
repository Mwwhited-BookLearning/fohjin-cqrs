namespace Fohjin.DDD.Commands;

public record ReportStolenBankCardCommand(Guid Id, Guid BankCardId) : CommandBase(Id);
