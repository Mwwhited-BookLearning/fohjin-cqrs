using Fohjin.DDD.Bus;
using Fohjin.DDD.Diagnostics;
using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.MessageRouting;
using Fohjin.DDD.Tests.Bus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Fohjin.DDD.Tests.Diagnostics;

// Proves EventSubscriptionBootstrapper's and DirectBus's new telemetry (Fohjin.DDD.Diagnostics.
// Telemetry) actually fires - real ActivityListener/MeterListener registrations, same
// instruments a live Aspire dashboard would show, over a real IBus/DI-wired pipeline rather
// than calling the private Subscribe<TEvent> method directly.
[TestClass]
[TestCategory("unit")]
public class EventTelemetryTest
{
    private readonly List<Activity> _activities = [];
    private readonly List<(string InstrumentName, object Value, KeyValuePair<string, object?>[] Tags)> _measurements = [];
    private ActivityListener _activityListener = null!;
    private MeterListener _meterListener = null!;

    [TestInitialize]
    public void Setup()
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == Telemetry.ServiceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _activities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == Telemetry.ServiceName)
                    listener.EnableMeasurementEvents(instrument);
            },
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            _measurements.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            _measurements.Add((instrument.Name, value, tags.ToArray())));
        _meterListener.Start();
    }

    [TestCleanup]
    public void TearDown()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    private static ServiceProvider BuildProvider(params IEventHandler[] handlers)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddBusServices();

        foreach (var handler in handlers)
            services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        provider.SubscribeEventHandlers();
        return provider;
    }

    [TestMethod]
    public async Task Then_publishing_one_event_records_one_published_and_one_handled_per_subscriber()
    {
        var first = new FirstTestEventHandler();
        var second = new SecondTestEventHandler();
        await using var provider = BuildProvider(first, second);
        var bus = provider.GetRequiredService<IBus>();
        var @event = new TestEvent();

        bus.Publish(new List<object> { @event });
        await bus.CommitAsync();
        await first.Signal.WaitAsync(TimeSpan.FromSeconds(5));
        await second.Signal.WaitAsync(TimeSpan.FromSeconds(5));

        var published = _measurements.Where(m => m.InstrumentName == "cqrs.events.published").ToList();
        Assert.AreEqual(1, published.Count);
        Assert.AreEqual(1L, published[0].Value);

        var handled = _measurements.Where(m => m.InstrumentName == "cqrs.events.handled").ToList();
        Assert.AreEqual(2, handled.Count, "one per subscribed handler");
        Assert.IsTrue(handled.All(m => m.Tags.Any(t => t.Key == "cqrs.outcome" && (string?)t.Value == "success")));

        Assert.AreEqual(2, _activities.Count(a => a.DisplayName == "event.handle TestEvent"));
    }

    [TestMethod]
    public async Task Then_a_failing_handler_records_failure_outcome_and_does_not_propagate()
    {
        var handler = new FirstTestEventHandler { ShouldThrow = true };
        await using var provider = BuildProvider(handler);
        var bus = provider.GetRequiredService<IBus>();

        bus.Publish(new List<object> { new TestEvent() });
        await bus.CommitAsync();
        await handler.Signal.WaitAsync(TimeSpan.FromSeconds(5));
        // EventSubscriptionBootstrapper.Subscribe logs and swallows - give the async
        // continuation a moment to reach the catch block/telemetry after Signal fires.
        await Task.Delay(100);

        var activity = _activities.Single(a => a.DisplayName == "event.handle TestEvent");
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual("failure", activity.GetTagItem("cqrs.outcome"));

        var handled = _measurements.Single(m => m.InstrumentName == "cqrs.events.handled");
        Assert.IsTrue(handled.Tags.Any(t => t.Key == "cqrs.outcome" && (string?)t.Value == "failure"));
    }
}
