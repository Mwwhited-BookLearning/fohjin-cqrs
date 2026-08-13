using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class AccountClosedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<AccountClosedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    // Used to delete the AccountReport/AccountDetailsReport rows and let
    // ClosedAccountCreatedEventHandler recreate them under a new id in a separate
    // ClosedAccountReport/ClosedAccountDetailsReport table. AccountReport/AccountDetailsReport
    // now carry their own Status, so the same row just gets marked closed in place instead -
    // this is also what lets LedgerReport have one unambiguous, never-changing FK target
    // (docs/08-reporting-read-models.md).
    public override async Task ExecuteAsync(AccountClosedEvent theEvent)
    {
        await _reportingRepository.UpdateAsync<AccountReport>(new { Status = "Closed" }, new { Id = theEvent.AggregateId });
        await _reportingRepository.UpdateAsync<AccountDetailsReport>(new { Status = "Closed" }, new { Id = theEvent.AggregateId });
    }
}