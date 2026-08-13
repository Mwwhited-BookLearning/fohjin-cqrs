using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Diagnostics;
using Fohjin.DDD.Tests.Bus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Fohjin.DDD.Tests.Diagnostics;

// Proves TransactionHandler's new telemetry (Fohjin.DDD.Diagnostics.Telemetry) actually fires,
// using real ActivityListener/MeterListener registrations rather than a full OTel SDK/exporter
// pipeline - the same instruments a live Aspire dashboard would show, just captured in-process.
[TestClass]
[TestCategory("unit")]
public class CommandTelemetryTest
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

    [TestMethod]
    public async Task Then_a_successful_command_records_one_activity_and_one_success_measurement()
    {
        var handler = new FirstTestCommandHandler();
        var transactionHandler = new TransactionHandler<TestCommand, FirstTestCommandHandler>(
            new NullUnitOfWork(), NullLogger<TransactionHandler<TestCommand, FirstTestCommandHandler>>.Instance);

        await transactionHandler.ExecuteAsync(new TestCommand(Guid.NewGuid()), handler);

        var activity = _activities.Single();
        Assert.AreEqual("command.execute TestCommand", activity.DisplayName);
        Assert.AreEqual("success", activity.GetTagItem("cqrs.outcome"));
        Assert.AreNotEqual(ActivityStatusCode.Error, activity.Status);

        var handled = _measurements.Single(m => m.InstrumentName == "cqrs.commands.handled");
        Assert.AreEqual(1L, handled.Value);
        Assert.IsTrue(handled.Tags.Any(t => t.Key == "cqrs.outcome" && (string?)t.Value == "success"));

        var duration = _measurements.Single(m => m.InstrumentName == "cqrs.command.duration");
        Assert.IsTrue((double)duration.Value >= 0);
    }

    [TestMethod]
    public async Task Then_a_failing_command_records_failure_outcome_and_still_rethrows()
    {
        var handler = new FirstTestCommandHandler { ShouldThrow = true };
        var transactionHandler = new TransactionHandler<TestCommand, FirstTestCommandHandler>(
            new NullUnitOfWork(), NullLogger<TransactionHandler<TestCommand, FirstTestCommandHandler>>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => transactionHandler.ExecuteAsync(new TestCommand(Guid.NewGuid()), handler));

        var activity = _activities.Single();
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual("failure", activity.GetTagItem("cqrs.outcome"));

        var handled = _measurements.Single(m => m.InstrumentName == "cqrs.commands.handled");
        Assert.IsTrue(handled.Tags.Any(t => t.Key == "cqrs.outcome" && (string?)t.Value == "failure"));
    }
}
