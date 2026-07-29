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
public class When_transfering_money_to_an_external_account : BaseTestFixture<MoneyTransferService>
{
    protected override void SetupDependencies()
    {
        OnDependency<IReportingRepository>()
            ?.Setup(x => x.GetByExampleAsync<AccountReport>(It.IsAny<object>()))
            .ReturnsAsync(new List<AccountReport> { new AccountReport(Guid.NewGuid(), Guid.NewGuid(), "AccountName", "target account number") });

        // !!! This is DEMO code !!!
        // Setup the SystemRandom class to return the value where the account is not found
        DoNotMock?.Add(typeof(ISystemRandom), new TestSystemRandom((min, max) => 2));
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
        OnDependency<IReceiveMoneyTransfers>()?.Verify(x => x.Receive(It.IsAny<MoneyTransfer>()));
    }

    protected override void Finally()
    {
    }
}