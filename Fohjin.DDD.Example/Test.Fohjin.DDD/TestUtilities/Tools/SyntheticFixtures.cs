using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.Domain.Client;
using Fohjin.DDD.Events.Client;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.EventStore.Storage.Memento;

namespace Test.Fohjin.DDD.TestUtilities.Tools;

// The generic reflection-based fill in TypeExtensions/TestDomainRepository can only ever
// produce independently-random leaf values, so it can never satisfy a handler's domain guard
// clause that correlates two fields (a command's referenced id against a nested collection on
// the aggregate it loads, or a balance against a withdrawal amount). For the handful of
// commands/events where that correlation actually matters, this builds a real, internally
// consistent aggregate via its own domain methods instead.
public static class SyntheticFixtures
{
    public static bool TryBuildConsistentCommand(Type commandType, out ICommand? command, out IDomainRepository<IDomainEvent>? repository)
    {
        if (commandType == typeof(AssignNewBankCardCommand))
        {
            var client = NewClient();
            var account = client.CreateNewAccount("Checking", "123456");
            command = new AssignNewBankCardCommand(client.Id, account.Id);
            repository = new SingleAggregateRepository(client);
            return true;
        }

        if (commandType == typeof(CancelBankCardCommand) || commandType == typeof(ReportStolenBankCardCommand))
        {
            var client = NewClient();
            var account = client.CreateNewAccount("Checking", "123456");
            client.AssignNewBankCardForAccount(account.Id);
            var bankCardId = ((IEventProvider<IDomainEvent>)client).GetChanges()
                .OfType<NewBankCardForAccountAsignedEvent>().Last().BankCardId;

            command = commandType == typeof(CancelBankCardCommand)
                ? new CancelBankCardCommand(client.Id, bankCardId)
                : new ReportStolenBankCardCommand(client.Id, bankCardId);
            repository = new SingleAggregateRepository(client);
            return true;
        }

        if (commandType == typeof(SendMoneyTransferCommand))
        {
            var account = NewFundedAccount();
            command = new SendMoneyTransferCommand(account.Id, 1.00M, "987654");
            repository = new SingleAggregateRepository(account);
            return true;
        }

        if (commandType == typeof(WithdrawalCashCommand))
        {
            var account = NewFundedAccount();
            command = new WithdrawalCashCommand(account.Id, 1.00M);
            repository = new SingleAggregateRepository(account);
            return true;
        }

        command = null;
        repository = null;
        return false;
    }

    private static Client NewClient() =>
        Client.CreateNew(new ClientName("Test Client"), new Address("Main St", "1", "12345", "Testville"), new PhoneNumber("555-0100"));

    private static ActiveAccount NewFundedAccount()
    {
        var account = ActiveAccount.CreateNew(Guid.NewGuid(), "Checking", "123456");
        account.Deposit(new Amount(100.00M));
        return account;
    }

    private class SingleAggregateRepository(object aggregate) : IDomainRepository<IDomainEvent>
    {
        private readonly object _aggregate = aggregate;

        public Task<TAggregate?> GetByIdAsync<TAggregate>(Guid id) where TAggregate : class, IOriginator, IEventProvider<IDomainEvent>, new() =>
            Task.FromResult(_aggregate as TAggregate);

        public void Add<TAggregate>(TAggregate aggregateRoot) where TAggregate : class, IOriginator, IEventProvider<IDomainEvent>, new() { }
    }
}
