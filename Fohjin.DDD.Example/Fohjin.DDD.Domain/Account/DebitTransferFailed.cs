namespace Fohjin.DDD.Domain.Account;

public class DebitTransferFailed(Amount amount, AccountNumber account) : Ledger(amount, account)
{
}