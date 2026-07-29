using Fohjin.DDD.Bus.Direct;
using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Configuration;
using Fohjin.DDD.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Fohjin.DDD.Bus;

[TestClass]
[TestCategory("unit")]
public class When_a_single_command_gets_published_to_the_bus_containing_multiple_command_handlers : BaseTestFixture<DirectBus>
{
    private FirstTestCommandHandler _handler = null!;
    private SecondTestCommandHandler _secondHandler = null!;
    private TestCommand _command = null!;

    protected override void SetupDependencies()
    {
        _handler = new FirstTestCommandHandler();
        _secondHandler = new SecondTestCommandHandler();
        Services.AddConfigurationServices()
            .AddTransient<ICommandHandler>(_ => _handler)
            .AddTransient<ICommandHandler>(_ => _secondHandler)
            .AddTransient(typeof(ITransactionHandler<,>), typeof(TransactionHandler<,>))
            .AddSingleton<IRouteMessages, MessageRouter>()
            .AddSingleton<IUnitOfWork, NullUnitOfWork>()
            ;

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
        SubjectUnderTest.Publish(new List<object> { _command });
        await SubjectUnderTest.CommitAsync();
        await _handler.Signal.WaitAsync(TimeSpan.FromSeconds(5));
        await _secondHandler.Signal.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void Then_the_execute_method_on_the_first_returned_command_handler_is_invoked_with_the_first_provided_command()
    {
        _handler?.Ids.First().WillBe(_command?.Id);
    }

    [TestMethod]
    public void Then_the_execute_method_on_the_second_returned_command_handler_is_invoked_with_the_first_provided_command()
    {
        _secondHandler?.Ids.First().WillBe(_command?.Id);
    }
}
