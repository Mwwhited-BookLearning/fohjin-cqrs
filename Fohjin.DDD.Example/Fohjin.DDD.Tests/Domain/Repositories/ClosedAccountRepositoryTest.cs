using Fohjin.DDD.Bootstrap;
using Fohjin.DDD.Bus;
using Fohjin.DDD.Common;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.EventStore.SqlServer;
using Fohjin.DDD.EventStore.Storage;
using Fohjin.DDD.EventStore.Storage.Memento;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;
using Fohjin.DDD.Tests.TestUtilities;

namespace Fohjin.DDD.Tests.Domain.Repositories;

[TestClass]
[TestCategory("unit")]
public class ClosedAccountRepositoryTest
{
    public TestContext TestContext { get; set; } = null!;
    public IServiceCollection Services { get; } = new ServiceCollection()
        .AddLogging(opt => opt.AddConsole().SetMinimumLevel(LogLevel.Information));

    public IServiceProvider Provider => field ??= Services.BuildServiceProvider();

    public ILogger<T> Logger<T>() => Provider.GetRequiredService<ILogger<T>>();

    private IDomainRepository<IDomainEvent> _repository = null!;
    private DomainEventStorage<IDomainEvent> _domainEventStorage = null!;
    private EventStoreIdentityMap<IDomainEvent> _eventStoreIdentityMap = null!;
    private EventStoreUnitOfWork<IDomainEvent> _eventStoreUnitOfWork = null!;
    private List<Ledger> _ledgers = null!;

    [TestInitialize]
    public async Task SetUp()
    {
        var connectionString = TestSqlServer.ConnectionStringFor(TestContext.GetDatabaseNameForTest("EventStore"));

        await new DomainDatabaseBootStrapper().ReCreateDatabaseSchema(connectionString);

        var dbContextOptions = new DbContextOptionsBuilder<DomainEventStoreDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _domainEventStorage = new DomainEventStorage<IDomainEvent>(
            new PooledDbContextFactory<DomainEventStoreDbContext>(dbContextOptions),
            new ExtendedFormatter()
            );

        _eventStoreIdentityMap = new EventStoreIdentityMap<IDomainEvent>();
        _eventStoreUnitOfWork = new EventStoreUnitOfWork<IDomainEvent>(
            _domainEventStorage,
            _eventStoreIdentityMap,
            new Mock<IBus>().Object,
            Logger<EventStoreUnitOfWork<IDomainEvent>>()
            );
        _repository = new DomainRepository<IDomainEvent>(
            _eventStoreUnitOfWork,
            _eventStoreIdentityMap,
            Logger<DomainRepository<IDomainEvent>>()
            );
    }

    [TestMethod]
    public async Task When_calling_Save_it_will_add_the_domain_events_to_the_domain_event_storage()
    {
        _ledgers = new List<Ledger>
        {
            new CreditMutation(1, new AccountNumber("0987654321")),
            new DebitMutation(1, new AccountNumber("0987654321")),
            new CreditTransfer(1, new AccountNumber("0987654321")),
            new DebitTransfer(1, new AccountNumber("0987654321")),
            new DebitTransferFailed(1, new AccountNumber("0987654321")),
        };

        var closedAccount = ClosedAccount.CreateNew(Guid.NewGuid(), Guid.NewGuid(), _ledgers, new AccountName("AccountName"), new AccountNumber("1234567890"));

        _repository?.Add(closedAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        Assert.AreEqual(1, (await _domainEventStorage!.GetEventsSinceLastSnapShotAsync(closedAccount.Id)).Count());
        Assert.AreEqual(1, (await _domainEventStorage!.GetAllEventsAsync(closedAccount.Id)).Count());
    }

    [TestMethod]
    public async Task When_calling_Save_it_will_reset_the_domain_events()
    {
        _ledgers = new List<Ledger>
        {
            new CreditMutation(1, new AccountNumber("0987654321")),
            new DebitMutation(1, new AccountNumber("0987654321")),
            new CreditTransfer(1, new AccountNumber("0987654321")),
            new DebitTransfer(1, new AccountNumber("0987654321")),
            new DebitTransferFailed(1, new AccountNumber("0987654321")),
        };

        var closedAccount = ClosedAccount.CreateNew(Guid.NewGuid(), Guid.NewGuid(), _ledgers, new AccountName("AccountName"), new AccountNumber("1234567890"));

        _repository?.Add(closedAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var closedAccountForRepository = (IEventProvider<IDomainEvent>)closedAccount;

        Assert.AreEqual(0, closedAccountForRepository.GetChanges().Count());
    }

    [TestMethod]
    public async Task When_calling_CreateMemento_it_will_return_a_closed_account_memento()
    {
        _ledgers = new List<Ledger>
        {
            new CreditMutation(1, new AccountNumber("0987654321")),
            new DebitMutation(1, new AccountNumber("0987654321")),
            new CreditTransfer(1, new AccountNumber("0987654321")),
            new DebitTransfer(1, new AccountNumber("0987654321")),
            new DebitTransferFailed(1, new AccountNumber("0987654321")),
        };

        var closedAccount = ClosedAccount.CreateNew(Guid.NewGuid(), Guid.NewGuid(), _ledgers, new AccountName("AccountName"), new AccountNumber("1234567890"));

        var memento = ((IOriginator)closedAccount).CreateMemento();

        var newClosedAccount = new ClosedAccount();

        ((IOriginator)newClosedAccount).SetMemento(memento);

        ClosedAccountComparer(closedAccount, newClosedAccount);
    }

    private static void ClosedAccountComparer(ClosedAccount original, ClosedAccount recreated)
    {
        var fields = typeof(ClosedAccount).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(List<Ledger>))
            {
                var counter = 0;
                var ledgers = field.GetValue(recreated) as List<Ledger>;
                foreach (var ledger in field.GetValue(original) as List<Ledger> ?? Enumerable.Empty<Ledger>())
                {
                    Assert.AreEqual(ledgers?[counter++].ToString(), ledger.ToString());
                }
                continue;
            }
            Assert.AreEqual(field.GetValue(recreated)?.ToString(), field.GetValue(original)?.ToString());
        }
    }


}