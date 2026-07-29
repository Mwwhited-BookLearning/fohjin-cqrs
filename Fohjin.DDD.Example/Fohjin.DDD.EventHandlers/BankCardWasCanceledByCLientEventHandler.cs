using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

// theEvent.AggregateId here is the bank card's own id (BankCardWasCanceledByClientEvent is
// applied from within BankCard.ClientCancelsBankCard, not Client) - matches BankCardReport.Id,
// set from BankCardId at Assign time (NewBankCardForAccountAssignedEventHandler).
public class BankCardWasCanceledByClientEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<BankCardWasCanceledByClientEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override Task ExecuteAsync(BankCardWasCanceledByClientEvent theEvent) =>
        _reportingRepository.UpdateAsync<BankCardReport>(new { Status = "Cancelled" }, new { Id = theEvent.AggregateId });
}