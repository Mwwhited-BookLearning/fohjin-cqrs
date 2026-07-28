using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class AccountNameChangedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<AccountNameChangedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override async Task ExecuteAsync(AccountNameChangedEvent theEvent)
    {
        await _reportingRepository.UpdateAsync<AccountReport>(new { theEvent.AccountName }, new { Id = theEvent.AggregateId });
        await _reportingRepository.UpdateAsync<AccountDetailsReport>(new { theEvent.AccountName }, new { Id = theEvent.AggregateId });
    }
}