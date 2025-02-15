using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers
{
    public class CashWithdrawnEventHandler : EventHandlerBase<CashWithdrawnEvent>
    {
        private readonly IReportingRepository _reportingRepository;

        public CashWithdrawnEventHandler(IReportingRepository reportingRepository)
        {
            _reportingRepository = reportingRepository;
        }

        public override Task ExecuteAsync(CashWithdrawnEvent theEvent)
        {
            _reportingRepository.Update<AccountDetailsReport>(new { theEvent.Balance }, new { Id = theEvent.AggregateId });
            _reportingRepository.Save(new LedgerReport(theEvent.Id, theEvent.AggregateId, "Withdrawal", theEvent.Amount));
            return Task.CompletedTask;
        }
    }
}