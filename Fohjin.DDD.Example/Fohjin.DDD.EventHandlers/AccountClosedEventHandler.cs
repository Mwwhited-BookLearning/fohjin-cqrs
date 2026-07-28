using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class AccountClosedEventHandler : EventHandlerBase<AccountClosedEvent>
{
    private readonly IReportingRepository _reportingRepository;

    public AccountClosedEventHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public override async Task ExecuteAsync(AccountClosedEvent theEvent)
    {
        await _reportingRepository.DeleteAsync<AccountReport>(new { Id = theEvent.AggregateId });
        await _reportingRepository.DeleteAsync<AccountDetailsReport>(new { Id = theEvent.AggregateId });
    }
}