using Fohjin.DDD.Bus;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Common;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Services;
using Fohjin.DDD.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Fohjin.DDD.Tests.TestUtilities;

namespace Fohjin.DDD.Tests.Scenarios.Transfering_money;

[TestClass]
[TestCategory("unit")]
public class When_failing_to_transfer_money_to_an_external_account : BaseTestFixture<MoneyTransferService>
{
    protected override void SetupDependencies()
    {
        OnDependency<IReceiveMoneyTransfers>()
            ?.Setup(x => x.Receive(It.IsAny<MoneyTransfer>()))
            .Throws(new UnknownAccountException("exception message"));

        // CompensatingActionBecauseOfFailedMoneyTransferAsync looks up the SOURCE account once
        // Receive() throws below - needs a real matching row now that Query<AccountReport>()
        // runs a real filter against this list, rather than GetByExampleAsync's old
        // It.IsAny<object>() mock, which returned the same canned account regardless of what
        // was actually asked.
        OnDependency<IReportingRepository>()
            ?.Setup(x => x.Query<AccountReport>())
            .Returns(new List<AccountReport>
            {
                new AccountReport(Guid.NewGuid(), Guid.NewGuid(), "Target Account", "target account number"),
                new AccountReport(Guid.NewGuid(), Guid.NewGuid(), "Source Account", "source account number"),
            }.AsQueryable());

        // !!! This is DEMO code !!!
        // Setup the SystemRandom class to return the value where the account is not found
        DoNotMock?.Add(typeof(ISystemRandom), new TestSystemRandom((min, max) => 4));
        DoNotMock?.Add(typeof(ISystemTimer), new TestSystemTimer());
    }

    protected override Task WhenAsync()
    {
        SubjectUnderTest?.Send(new MoneyTransfer("source account number", "target account number", 123.45M));
        return Task.CompletedTask;
    }

    [TestMethod]
    public void Then_the_newly_created_account_will_be_saved()
    {
        OnDependency<IBus>()?.Verify(x => x.Publish(It.IsAny<MoneyTransferFailedCompensatingCommand>()));
    }

    protected override void Finally()
    {
    }
}