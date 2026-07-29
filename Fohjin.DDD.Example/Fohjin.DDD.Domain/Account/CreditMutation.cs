namespace Fohjin.DDD.Domain.Account;

public class CreditMutation(Amount amount, AccountNumber account) : Ledger(amount, account)
{
}