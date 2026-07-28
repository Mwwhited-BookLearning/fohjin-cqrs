using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers
{
    public class CashDepositEventHandler : EventHandlerBase<CashDepositedEvent>
    {
        private readonly IReportingRepository _reportingRepository;

        public CashDepositEventHandler(IReportingRepository reportingRepository)
        {
            _reportingRepository = reportingRepository;
        }

        public override async Task ExecuteAsync(CashDepositedEvent theEvent)
        {
            await _reportingRepository.UpdateAsync<AccountDetailsReport>(new { theEvent.Balance }, new { Id = theEvent.AggregateId });
            await _reportingRepository.SaveAsync(new LedgerReport(theEvent.Id, theEvent.AggregateId, "Deposit", theEvent.Amount));
        }
    }
}