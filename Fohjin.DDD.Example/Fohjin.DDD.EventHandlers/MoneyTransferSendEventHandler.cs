using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers
{
    public class MoneyTransferSendEventHandler : EventHandlerBase<MoneyTransferSendEvent>
    {
        private readonly IReportingRepository _reportingRepository;

        public MoneyTransferSendEventHandler(IReportingRepository reportingRepository)
        {
            _reportingRepository = reportingRepository;
        }

        public override Task ExecuteAsync(MoneyTransferSendEvent theEvent)
        {
            _reportingRepository.Update<AccountDetailsReport>(new { theEvent.Balance }, new { Id = theEvent.AggregateId });
            _reportingRepository.Save(new LedgerReport(theEvent.Id, theEvent.AggregateId, string.Format("Transfer to {0}", theEvent.TargetAccount), theEvent.Amount));

            return Task.CompletedTask;
        }
    }
}