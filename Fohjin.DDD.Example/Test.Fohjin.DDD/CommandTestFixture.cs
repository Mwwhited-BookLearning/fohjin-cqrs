using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Commands;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.EventStore.Storage.Memento;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test.Fohjin.DDD;

[TestClass]
[TestCategory("unit")]
public abstract class CommandTestFixture<TCommand, TCommandHandler, TAggregateRoot>
    where TCommand : class, ICommand
    where TCommandHandler : class, ICommandHandler<TCommand>
    where TAggregateRoot : class, IOriginator, IEventProvider<IDomainEvent>, new()
{
    private IDictionary<Type, object> mocks = null!;

    protected TAggregateRoot AggregateRoot = null!;
    protected ICommandHandler<TCommand> CommandHandler = null!;
    protected Exception CaughtException = null!;
    protected IEnumerable<IDomainEvent> PublishedEvents = null!;
    protected virtual void SetupDependencies() { }
    protected virtual IEnumerable<IDomainEvent> Given() 
    {
        return new List<IDomainEvent>();
    }
    protected virtual void Finally() { }
    protected abstract TCommand When();

    [TestInitialize]
    public async Task Setup()
    {
        mocks = new Dictionary<Type, object>();
        CaughtException = new ThereWasNoExceptionButOneWasExpectedException();
        AggregateRoot = new TAggregateRoot();
        AggregateRoot.LoadFromHistory(Given());

        CommandHandler = BuildCommandHandler();

        SetupDependencies();
        try
        {
             await CommandHandler.ExecuteAsync(When());
            PublishedEvents = AggregateRoot.GetChanges();
        }
        catch (Exception exception)
        {
            CaughtException = exception;
        }
        finally
        {
            Finally();
        }
    }

    public Mock<TType> OnDependency<TType>() where TType : class
    {
        return (Mock<TType>)mocks?[typeof(TType)]!;
    }

    private ICommandHandler<TCommand> BuildCommandHandler()
    {
        var constructorInfo = typeof(TCommandHandler).GetConstructors().First();

        foreach (var parameter in constructorInfo.GetParameters())
        {
            if (parameter.ParameterType == typeof(IDomainRepository<IDomainEvent>))
            {
                var repositoryMock = new Mock<IDomainRepository<IDomainEvent>>();
                repositoryMock.Setup(x => x.GetByIdAsync<TAggregateRoot>(It.IsAny<Guid>())).ReturnsAsync(AggregateRoot);
                repositoryMock.Setup(x => x.Add(It.IsAny<TAggregateRoot>())).Callback<TAggregateRoot>(x => AggregateRoot = x);
                mocks?.Add(parameter.ParameterType, repositoryMock);
                continue;
            }

            mocks?.Add(parameter.ParameterType, CreateMock(parameter.ParameterType));
        }

        return (ICommandHandler<TCommand>)constructorInfo.Invoke(mocks?.Values.Select(x => ((Mock) x).Object).ToArray())!;
    }

    private static object CreateMock(Type type)
    {
        var constructorInfo = typeof (Mock<>).MakeGenericType(type).GetConstructors().First();
        return constructorInfo.Invoke([]);
    }
}

public class ThereWasNoExceptionButOneWasExpectedException : Exception {}

public class PrepareDomainEvent
{
    public static EventVersionSetter Set(IDomainEvent domainEvent)
    {
        return new EventVersionSetter(domainEvent);
    }
}

public class EventVersionSetter(IDomainEvent domainEvent)
{
    private readonly IDomainEvent _domainEvent = domainEvent;

    public IDomainEvent ToVersion(int version)
    {
        _domainEvent.Version = version;
        return _domainEvent;
    }
}