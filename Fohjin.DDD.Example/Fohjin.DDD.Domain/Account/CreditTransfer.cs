namespace Fohjin.DDD.Domain.Account;

public class CreditTransfer(Amount amount, AccountNumber account) : Ledger(amount, account)
{
}