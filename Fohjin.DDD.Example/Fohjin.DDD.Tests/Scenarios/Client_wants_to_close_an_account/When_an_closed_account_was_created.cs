using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.Events.Account;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fohjin.DDD.Tests.Scenarios.Client_wants_to_close_an_account;

// ClosedAccountCreatedEventHandler is a no-op on the read-model side now: AccountClosedEventHandler
// already marked the same AccountReport/AccountDetailsReport row Status = "Closed" in place, and
// the original LedgerReport rows from the account's open period are never deleted, so there's
// nothing left to (re)create under this event's own (different) AccountId - see
// docs/08-reporting-read-models.md.
[TestClass]
[TestCategory("unit")]
public class When_an_closed_account_was_created : EventTestFixture<ClosedAccountCreatedEvent, ClosedAccountCreatedEventHandler>
{
    protected override ClosedAccountCreatedEvent When()
    {
        var ledgers = new List<KeyValuePair<string, string>>
        {
            new("CreditMutation", "10.5|"),
        };

        return new ClosedAccountCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ledgers, "Closed Account", "1234567890");
    }

    [TestMethod]
    public void Then_it_will_not_throw_an_exception()
    {
        CaughtException.WillBeOfType<ThereWasNoExceptionButOneWasExpectedException>();
    }
}
