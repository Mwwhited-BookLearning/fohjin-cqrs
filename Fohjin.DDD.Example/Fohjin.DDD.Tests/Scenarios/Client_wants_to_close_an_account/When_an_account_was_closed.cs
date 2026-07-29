using System;
using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Fohjin.DDD.Tests.Scenarios.Client_wants_to_close_an_account;

[TestClass]
[TestCategory("unit")]
public class When_an_account_was_closed : EventTestFixture<AccountClosedEvent, AccountClosedEventHandler>
{
    protected override AccountClosedEvent When()
    {
        return new AccountClosedEvent { AggregateId = Guid.NewGuid() };
    }

    [TestMethod]
    public void Then_the_reporting_repository_will_be_used_to_update_the_account_report()
    {
        OnDependency<IReportingRepository>().Verify(x => x.DeleteAsync<AccountReport>(It.IsAny<object>()), Times.Once());
    }

    [TestMethod]
    public void Then_the_reporting_repository_will_be_used_to_update_the_account_details_report()
    {
        OnDependency<IReportingRepository>().Verify(x => x.DeleteAsync<AccountDetailsReport>(It.IsAny<object>()), Times.Once());
    }
}