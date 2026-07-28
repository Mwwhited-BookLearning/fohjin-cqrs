namespace Fohjin.DDD.Domain.Account;

public class DebitTransfer(Amount amount, AccountNumber account) : Ledger(amount, account)
{
}