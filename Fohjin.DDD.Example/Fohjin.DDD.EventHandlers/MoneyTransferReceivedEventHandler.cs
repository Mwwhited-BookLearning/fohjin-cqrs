using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class MoneyTransferReceivedEventHandler : EventHandlerBase<MoneyTransferReceivedEvent>
{
    private readonly IReportingRepository _reportingRepository;

    public MoneyTransferReceivedEventHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public override async Task ExecuteAsync(MoneyTransferReceivedEvent theEvent)
    {
        await _reportingRepository.UpdateAsync<AccountDetailsReport>(new { theEvent.Balance }, new { Id = theEvent.AggregateId });
        await _reportingRepository.SaveAsync(new LedgerReport(theEvent.Id, theEvent.AggregateId, string.Format("Transfer from {0}", theEvent.SourceAccount), theEvent.Amount));
    }
}