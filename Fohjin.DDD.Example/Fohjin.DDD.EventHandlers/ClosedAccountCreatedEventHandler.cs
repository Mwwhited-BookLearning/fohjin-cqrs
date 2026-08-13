using Fohjin.DDD.Events.Account;

namespace Fohjin.DDD.EventHandlers;

// A no-op on the read-model side. AccountReport/AccountDetailsReport are marked Status =
// "Closed" in place by AccountClosedEventHandler, which fires first in the same commit
// (CloseAccountCommandHandler calls ActiveAccount.Close() - which applies AccountClosedEvent
// to the still-tracked ActiveAccount - before it registers the new ClosedAccount aggregate
// this event belongs to). There is no longer a separate ClosedAccountReport/
// ClosedAccountDetailsReport row to create, and the original LedgerReport rows from the
// account's open period are never deleted, so there's nothing left to (re)project here either.
// Kept as a registered handler - rather than deleted outright - only because
// Fohjin.DDD.Tests/Events/All_domain_events_must_have_a_handler.cs asserts every domain event
// has one.
public class ClosedAccountCreatedEventHandler : EventHandlerBase<ClosedAccountCreatedEvent>
{
    public override Task ExecuteAsync(ClosedAccountCreatedEvent theEvent) => Task.CompletedTask;
}
