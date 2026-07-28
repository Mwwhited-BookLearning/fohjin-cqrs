using Fohjin.DDD.BankApplication;
using Fohjin.DDD.Bus;
using Fohjin.DDD.Common;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.Domain.Mementos;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.EventStore.SQLite;
using Fohjin.DDD.EventStore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Test.Fohjin.DDD.TestUtilities;

namespace Test.Fohjin.DDD.Domain.Repositories;

[TestClass]
[TestCategory("unit")]
public class ActiveAccountRepositoryTest
{
    public TestContext TestContext { get; set; } = null!;
    public IServiceCollection Services { get; } = new ServiceCollection()
        .AddLogging(opt => opt.AddConsole().SetMinimumLevel(LogLevel.Information));

    public IServiceProvider Provider => field ??= Services.BuildServiceProvider();

    public ILogger<T> Logger<T>() => Provider.GetRequiredService<ILogger<T>>();

    private IDomainRepository<IDomainEvent> _repository;
    private DomainEventStorage<IDomainEvent> _domainEventStorage;
    private EventStoreIdentityMap<IDomainEvent> _eventStoreIdentityMap;
    private EventStoreUnitOfWork<IDomainEvent> _eventStoreUnitOfWork;

    [TestInitialize]
    public async Task SetUp()
    {
        TestContext.SetupWorkingDirectory();
        var dataBaseFile = Path.Combine(
            (string)TestContext.Properties[TestContextExtensions.TestWorkingDirectory] ??
                throw new NotSupportedException($"TestContext Property is missing {nameof(TestContextExtensions.TestWorkingDirectory)}"),
            DomainDatabaseBootStrapper.DataBaseFile
            );

        await new DomainDatabaseBootStrapper().ReCreateDatabaseSchema(dataBaseFile);

        var sqliteConnectionString = string.Format("Data Source={0}", dataBaseFile);

        var dbContextOptions = new DbContextOptionsBuilder<DomainEventStoreDbContext>()
            .UseSqlite(sqliteConnectionString)
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
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        Assert.AreEqual(3, (await _domainEventStorage!.GetEventsSinceLastSnapShotAsync(activeAccount.Id)).Count());
        Assert.AreEqual(3, (await _domainEventStorage!.GetAllEventsAsync(activeAccount.Id)).Count());
    }

    [TestMethod]
    public async Task When_calling_Save_it_will_reset_the_domain_events()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var activeAccountForRepository = (IEventProvider<IDomainEvent>)activeAccount;

        Assert.AreEqual(0, activeAccountForRepository.GetChanges().Count());
    }

    [TestMethod]
    public async Task When_calling_Save_after_more_than_9_events_a_new_snap_shot_will_be_created_9_events_will_not()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        Assert.IsNull((await _domainEventStorage!.GetSnapShotAsync(activeAccount.Id)));
    }

    [TestMethod]
    public async Task When_calling_Save_after_more_than_9_events_a_new_snap_shot_will_be_created_10_events()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();
        await _domainEventStorage!.SaveShapShotAsync(activeAccount);

        var snapShot = (await _domainEventStorage!.GetSnapShotAsync(activeAccount.Id));

        Assert.IsNotNull(snapShot);
        Assert.IsInstanceOfType<ActiveAccountMemento>(snapShot.Memento);
    }

    [TestMethod]
    public async Task When_calling_Save_after_more_than_9_events_a_new_snap_shot_will_be_created_11_events()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();
        await _domainEventStorage!.SaveShapShotAsync(activeAccount);

        var snapShot = (await _domainEventStorage!.GetSnapShotAsync(activeAccount.Id));

        Assert.IsNotNull(snapShot);
        Assert.IsInstanceOfType<ActiveAccountMemento>(snapShot?.Memento);
    }

    [TestMethod]
    public async Task When_calling_Save_after_more_than_9_events_after_the_last_snap_shot_a_new_snapshot_will_be_created_10_events_after_last_snapshot()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();
        await _domainEventStorage!.SaveShapShotAsync(activeAccount);

        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var snapShot = (await _domainEventStorage!.GetSnapShotAsync(activeAccount.Id));

        Assert.IsNotNull(snapShot);
        Assert.IsInstanceOfType<ActiveAccountMemento>(snapShot?.Memento);
    }

    [TestMethod]
    public async Task When_calling_Save_after_more_than_9_events_after_the_last_snap_shot_a_new_snapshot_will_be_created_10_events_after_last_snapshot_9_events_after_last_snapshot()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();
        await _domainEventStorage!.SaveShapShotAsync(activeAccount);

        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var snapShot = (await _domainEventStorage!.GetSnapShotAsync(activeAccount.Id));

        Assert.IsNotNull(snapShot);
        Assert.IsInstanceOfType<ActiveAccountMemento>(snapShot?.Memento);
    }

    [TestMethod]
    public async Task When_calling_Save_after_more_than_9_events_after_the_last_snap_shot_a_new_snapshot_will_be_created_10_events_after_last_snapshot_9_events_after_last_snapshot_verify_all_event_counts()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();
        await _domainEventStorage!.SaveShapShotAsync(activeAccount);

        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(1));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();


        Assert.AreEqual(9, (await _domainEventStorage!.GetEventsSinceLastSnapShotAsync(activeAccount.Id)).Count());
        Assert.AreEqual(19, (await _domainEventStorage!.GetAllEventsAsync(activeAccount.Id)).Count());
    }

    [TestMethod]
    public async Task When_calling_GetById_after_9_events_a_new_ActiveAcount_will_be_populated()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(2));
        activeAccount.Deposit(new Amount(3));
        activeAccount.Deposit(new Amount(4));
        activeAccount.Deposit(new Amount(5));
        activeAccount.Deposit(new Amount(6));
        activeAccount.Deposit(new Amount(7));
        activeAccount.Deposit(new Amount(8));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var sut = (await _repository!.GetByIdAsync<ActiveAccount>(activeAccount.Id));

        try
        {
            sut?.Withdrawal(new Amount(36));
        }
        catch (Exception Ex)
        {
            Assert.Fail(string.Format("This should not fail: {0}", Ex.Message));
        }

        Assert.ThrowsExactly<AccountBalanceToLowException>(() =>
        {
            sut?.Withdrawal(new Amount(1));
        });
    }

    [TestMethod]
    public async Task When_calling_GetById_after_every_10_events_a_new_snap_shot_will_be_created()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(2));
        activeAccount.Deposit(new Amount(3));
        activeAccount.Deposit(new Amount(4));
        activeAccount.Deposit(new Amount(5));
        activeAccount.Deposit(new Amount(6));
        activeAccount.Deposit(new Amount(7));
        activeAccount.Deposit(new Amount(8));
        activeAccount.Deposit(new Amount(9));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var sut = (await _repository!.GetByIdAsync<ActiveAccount>(activeAccount.Id));

        try
        {
            sut?.Withdrawal(new Amount(45));
        }
        catch (Exception Ex)
        {
            Assert.Fail(string.Format("This should not fail: {0}", Ex.Message));
        }

        Assert.ThrowsExactly<AccountBalanceToLowException>(() =>
        {
            sut?.Withdrawal(new Amount(1));
        });
    }

    [TestMethod]
    public async Task When_calling_GetById_after_every_10_events_a_new_snap_shot_will_be_created_11_events()
    {
        var activeAccount = ActiveAccount.CreateNew(Guid.NewGuid(), "AccountName", "Account Number");
        activeAccount.Deposit(new Amount(1));
        activeAccount.Deposit(new Amount(2));
        activeAccount.Deposit(new Amount(3));
        activeAccount.Deposit(new Amount(4));
        activeAccount.Deposit(new Amount(5));
        activeAccount.Deposit(new Amount(6));
        activeAccount.Deposit(new Amount(7));
        activeAccount.Deposit(new Amount(8));
        activeAccount.Deposit(new Amount(9));
        activeAccount.Deposit(new Amount(10));

        _repository?.Add(activeAccount);
        await _eventStoreUnitOfWork!.CommitAsync();

        var sut = (await _repository!.GetByIdAsync<ActiveAccount>(activeAccount.Id));

        try
        {
            sut?.Withdrawal(new Amount(55));
        }
        catch (Exception Ex)
        {
            Assert.Fail(string.Format("This should not fail: {0}", Ex.Message));
        }

        Assert.ThrowsExactly<AccountBalanceToLowException>(() =>
        {
            sut?.Withdrawal(new Amount(1));
        });
    }
}