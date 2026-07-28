using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;

namespace Fohjin.DDD.EventHandlers;

public class BankCardWasReportedStolenEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<BankCardWasReportedStolenEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override Task ExecuteAsync(BankCardWasReportedStolenEvent theEvent) =>
        Task.CompletedTask;
}