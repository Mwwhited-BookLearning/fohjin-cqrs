using Fohjin.DDD.EventStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.TestUtilities.Tools;

public class TestDomainRepository<TDomainEvent>(
    TestContext testContext,
    IServiceProvider serviceProvider
        ) : IDomainRepository<TDomainEvent>
     where TDomainEvent : IDomainEvent
{
    private readonly TestContext _testContext = testContext;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    void IDomainRepository<TDomainEvent>.Add<TAggregate>(TAggregate aggregateRoot)
    {
        _testContext.AddResults(typeof(TAggregate).Name, aggregateRoot);
    }

    Task<TAggregate?> IDomainRepository<TDomainEvent>.GetByIdAsync<TAggregate>(Guid id)
        where TAggregate : class
    {
        var aggregate = (TAggregate)typeof(TAggregate).FillObject(_serviceProvider);
        aggregate.Id = id;
        return Task.FromResult<TAggregate?>(aggregate);
    }
}
