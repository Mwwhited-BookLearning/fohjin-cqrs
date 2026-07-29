using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class AccountOpenedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<AccountOpenedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override async Task ExecuteAsync(AccountOpenedEvent theEvent)
    {
        var account = new AccountReport(theEvent.AccountId, theEvent.ClientId, theEvent.AccountName, theEvent.AccountNumber);
        var accountDetails = new AccountDetailsReport(theEvent.AccountId, theEvent.ClientId, theEvent.AccountName, 0.0M, theEvent.AccountNumber);
        await _reportingRepository.SaveAsync(account);
        await _reportingRepository.SaveAsync(accountDetails);
    }
}