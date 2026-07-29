namespace Fohjin.DDD.Domain.Account;

public class DebitMutation(Amount amount, AccountNumber account) : Ledger(amount, account)
{
}