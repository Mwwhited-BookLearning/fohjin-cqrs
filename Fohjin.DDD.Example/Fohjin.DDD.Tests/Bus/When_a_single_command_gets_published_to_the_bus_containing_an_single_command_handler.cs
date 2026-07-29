using Fohjin.DDD.Bus.Direct;
using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.MessageRouting;
using Fohjin.DDD.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.Bus;

[TestClass]
[TestCategory("unit")]
public class When_a_single_command_gets_published_to_the_bus_containing_an_single_command_handler : BaseTestFixture<DirectBus>
{
    private FirstTestCommandHandler _handler = null!;
    private TestCommand _command = null!;

    protected override void SetupDependencies()
    {
        _handler = new FirstTestCommandHandler();
        Services.AddMessageRoutingServices()
            .AddTransient<ICommandHandler>(_ => _handler)
            .AddTransient(typeof(ITransactionHandler<,>), typeof(TransactionHandler<,>))
            .AddSingleton<IRouteMessages, MessageRouter>()
            .AddSingleton<IUnitOfWork, NullUnitOfWork>()
            ;

        // DirectBus resolves IRouteMessages lazily off the IServiceProvider passed to its own
        // constructor - that has to be the real, fully-configured Provider (not an auto-mocked one)
        // for the command pipeline above to actually be reachable.
        DoNotMock?.Add(typeof(IServiceProvider), this.Provider);
        DoNotMock?.Add(typeof(IQueue), new InMemoryQueue(this.Logger<InMemoryQueue>()));
    }

    protected override void Given()
    {
        _command = new TestCommand(Guid.NewGuid());
    }

    protected override async Task WhenAsync()
    {
        if (SubjectUnderTest == null || _command == null)
            return;

        SubjectUnderTest.Publish(_command);
        await SubjectUnderTest.CommitAsync();
        await _handler.Signal.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void Then_the_execute_method_on_the_returned_command_handler_is_invoked_with_the_provided_command()
    {
        _handler?.Ids.First().WillBe(_command?.Id);
    }
}
