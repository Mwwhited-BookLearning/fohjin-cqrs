using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

// theEvent.AggregateId here is the Client's id (NewBankCardForAccountAsignedEvent is applied
// from within Client.AssignNewBankCardForAccount, so Apply() stamps the Client's own Id) - the
// only place this event carries the bank card's own id is BankCardId. Contrast with the two
// "disabled" events below, whose AggregateId is the bank card's id instead (Apply runs from
// within BankCard itself for those).
public class NewBankCardForAccountAssignedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<NewBankCardForAccountAsignedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override Task ExecuteAsync(NewBankCardForAccountAsignedEvent theEvent) =>
        _reportingRepository.SaveAsync(new BankCardReport(theEvent.BankCardId, theEvent.AggregateId, theEvent.AccountId, "Active"));
}