using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

// theEvent.AggregateId here is the bank card's own id - see BankCardWasCanceledByClientEventHandler's comment.
public class BankCardWasReportedStolenEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<BankCardWasReportedStolenEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override Task ExecuteAsync(BankCardWasReportedStolenEvent theEvent) =>
        _reportingRepository.UpdateAsync<BankCardReport>(new { Status = "ReportedStolen" }, new { Id = theEvent.AggregateId });
}