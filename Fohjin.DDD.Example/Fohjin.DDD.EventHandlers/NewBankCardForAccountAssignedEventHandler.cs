using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;

namespace Fohjin.DDD.EventHandlers;

public class NewBankCardForAccountAssignedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<NewBankCardForAccountAsignedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override Task ExecuteAsync(NewBankCardForAccountAsignedEvent theEvent) =>
        Task.CompletedTask;
}