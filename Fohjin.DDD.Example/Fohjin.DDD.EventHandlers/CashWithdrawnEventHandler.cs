using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class CashWithdrawnEventHandler : EventHandlerBase<CashWithdrawnEvent>
{
    private readonly IReportingRepository _reportingRepository;

    public CashWithdrawnEventHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public override async Task ExecuteAsync(CashWithdrawnEvent theEvent)
    {
        await _reportingRepository.UpdateAsync<AccountDetailsReport>(new { theEvent.Balance }, new { Id = theEvent.AggregateId });
        await _reportingRepository.SaveAsync(new LedgerReport(theEvent.Id, theEvent.AggregateId, "Withdrawal", theEvent.Amount));
    }
}