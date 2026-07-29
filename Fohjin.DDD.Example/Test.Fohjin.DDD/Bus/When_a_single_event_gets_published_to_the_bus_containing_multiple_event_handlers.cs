using Fohjin.DDD.Bus.Direct;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reactive.Linq;

namespace Test.Fohjin.DDD.Bus;

[TestClass]
[TestCategory("unit")]
public class When_a_single_event_gets_published_to_the_bus_containing_multiple_event_handlers : BaseTestFixture<DirectBus>
{
    private FirstTestEventHandler _handler = null!;
    private SecondTestEventHandler _secondHandler = null!;
    private TestEvent _event = null!;

    protected override void SetupDependencies()
    {
        _handler = new FirstTestEventHandler();
        _secondHandler = new SecondTestEventHandler();
        DoNotMock?.Add(typeof(IQueue), new InMemoryQueue(this.Logger<InMemoryQueue>()));
    }

    protected override void Given()
    {
        _event = new TestEvent();
        SubjectUnderTest.Events.OfType<TestEvent>().Subscribe(async e => await _handler.ExecuteAsync(e));
        SubjectUnderTest.Events.OfType<TestEvent>().Subscribe(async e => await _secondHandler.ExecuteAsync(e));
    }

    protected override async Task WhenAsync()
    {
        if (SubjectUnderTest == null || _event == null)
            return;
        SubjectUnderTest.Publish(new List<object> { _event });
        await SubjectUnderTest.CommitAsync();
        await _handler.Signal.WaitAsync(TimeSpan.FromSeconds(5));
        await _secondHandler.Signal.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void Then_the_execute_method_on_the_first_returned_event_handler_is_invoked_with_the_first_provided_event()
    {
        _handler?.Ids.First().WillBe(_event?.Id);
    }

    [TestMethod]
    public void Then_the_execute_method_on_the_second_returned_event_handler_is_invoked_with_the_first_provided_event()
    {
        _secondHandler?.Ids.First().WillBe(_event?.Id);
    }
}
