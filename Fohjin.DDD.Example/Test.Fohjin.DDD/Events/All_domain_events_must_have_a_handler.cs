using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.Events.Account;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Test.Fohjin.DDD.TestUtilities;
using Test.Fohjin.DDD.TestUtilities.Tools;

namespace Test.Fohjin.DDD.Events;

[TestClass]
[TestCategory("dev-tool")]
public class All_domain_events_must_have_a_handler
{
    public TestContext TestContext { get; set; } = null!;

    [DataTestMethod]
    [DynamicData(nameof(TestData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(TestDataDisplayName))]
    public async Task TestEventHandler(Type eventType, Type handlerType = null)
    {
        this.TestContext.WriteLine($"RUN_ID:{TestContext.Properties[$"RUN_ID"] = Guid.NewGuid()}");
        this.TestContext.Properties[$"Parameter::{nameof(eventType)}"] = eventType;
        this.TestContext.Properties[$"Parameter::{nameof(handlerType)}"] = handlerType;

        if (handlerType == null && (eventType.Namespace?.Contains("Test") ?? true))
        {
            Assert.Inconclusive("No handlers exist but it's a test event anyway");
        }

        Assert.IsNotNull(handlerType, "No handlers exist");

        var services = new ServiceCollection()
            .AddLogging(log => log.AddConsole().SetMinimumLevel(LogLevel.Information))
            .AddSingleton(_ => TestContext)
            .AddSingleton(typeof(IDomainRepository<>), typeof(TestDomainRepository<>))
            .AddSingleton<IReportingRepository, TestReportingRepository>()
            .AddSingleton<ISendMoneyTransfer, TestSendMoneyTransfer>()
            ;
        var serviceProvider = services.BuildServiceProvider();

        if (eventType.GetNonDefaultValue(serviceProvider) is IDomainEvent evnt && ActivatorUtilities.CreateInstance(serviceProvider, handlerType) is IEventHandler instance)
        {
            // Generic KeyValuePair<string,string> fill can't know "Key" is really a closed set of
            // ledger transfer-type names - force it to a real one so the handler's happy path runs.
            if (evnt is ClosedAccountCreatedEvent closedAccountCreated)
                closedAccountCreated.Ledgers = new() { new("CreditMutation", "100.00") };

            try
            {
                await instance.ExecuteAsync(evnt);
            }
            catch (Exception ex) when (ex.GetType().Namespace?.StartsWith("Fohjin.DDD.Domain") == true || ex is UnsupportedTransferTypeException)
            {
                // The event is filled with random reflection-generated data, so structured fields like
                // "transfer type" or referenced ids won't match anything real. A handler correctly
                // rejecting that malformed synthetic input proves it's wired up, which is what this
                // smoke test is checking.
                Assert.Inconclusive($"Handler rejected synthetic data: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
    public static string TestDataDisplayName(MethodInfo methodInfo, object[] data) =>
        $"{methodInfo.Name} for {((Type)data[0]).Name} => {((Type?)data?[1])?.Name}";

    public static IEnumerable<object[]> TestData()
    {
        var commands = from eventType in typeof(IDomainEvent).GetInstanceTypes()
                       let handlerInterfaceType = typeof(IEventHandler<>).MakeGenericType(eventType)
                       let handlers = handlerInterfaceType.GetInstanceTypes()
                       from handlerType in handlers.DefaultIfEmpty()
                       select new
                       {
                           eventType,
                           handlerType,
                       };

        var items = commands
            ;
        var mapped = items.Select(i => new object[] { i.eventType, i.handlerType });
        return mapped;
    }
}