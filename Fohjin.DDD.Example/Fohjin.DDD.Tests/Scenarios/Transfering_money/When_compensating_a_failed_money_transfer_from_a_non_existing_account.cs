using System;
using Fohjin.DDD.CommandHandlers;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.Scenarios.Transfering_money;

[TestClass]
[TestCategory("unit")]
public class When_compensating_a_failed_money_transfer_from_a_non_existing_account : CommandTestFixture<MoneyTransferFailedCompensatingCommand, MoneyTransferFailedCompensatingCommandHandler, ActiveAccount>
{
    protected override MoneyTransferFailedCompensatingCommand When()
    {
        return new MoneyTransferFailedCompensatingCommand(Guid.NewGuid(), 5.0M, "0987654321");
    }

    [TestMethod]
    public void Then_a_non_existing_account_exception_will_be_thrown()
    {
        CaughtException.WillBeOfType<NonExitsingAccountException>();
    }

    [TestMethod]
    public void Then_the_exception_message_will_be()
    {
        CaughtException.Message.WillBe("The ActiveAccount is not created and no operations can be executed on it");
    }
}