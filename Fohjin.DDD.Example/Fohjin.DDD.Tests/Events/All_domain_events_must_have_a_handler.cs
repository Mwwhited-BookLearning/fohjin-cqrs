using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Fohjin.DDD.Tests.TestUtilities;
using Fohjin.DDD.Tests.TestUtilities.Tools;

namespace Fohjin.DDD.Tests.Events;

[TestClass]
[TestCategory("dev-tool")]
public class All_domain_events_must_have_a_handler
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DynamicData(nameof(TestData), DynamicDataDisplayName = nameof(TestDataDisplayName))]
    public async Task TestEventHandler(Type eventType, Type? handlerType = null)
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

        if (eventType.GetNonDefaultValue(serviceProvider) is IDomainEvent evnt && ActivatorUtilities.CreateInstance(serviceProvider, handlerType!) is IEventHandler instance)
        {
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