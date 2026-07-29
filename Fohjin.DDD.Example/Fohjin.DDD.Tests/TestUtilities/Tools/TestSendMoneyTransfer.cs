using Fohjin.DDD.Services;
using Fohjin.DDD.Services.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.TestUtilities.Tools;

public class TestSendMoneyTransfer(
    TestContext testContext
        ) : ISendMoneyTransfer
{
    private readonly TestContext _testContext = testContext;

    public void Send(MoneyTransfer moneyTransfer)
    {
        _testContext.AddResults("Send-MoneyTransfer", moneyTransfer);
    }
}