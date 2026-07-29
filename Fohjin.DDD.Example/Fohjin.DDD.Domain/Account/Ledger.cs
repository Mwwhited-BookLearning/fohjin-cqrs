namespace Fohjin.DDD.Domain.Account;

public abstract class Ledger(Amount amount, AccountNumber account)
{
    public Amount Amount { get; init; } = amount;
    public AccountNumber Account { get; init; } = account;

    public override string ToString() =>
        string.Join(" - ", GetType().Name, Account.Number, (decimal)Amount);
}