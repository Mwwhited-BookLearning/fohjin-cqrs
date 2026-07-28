using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class ClientNameChangedEventHandler : EventHandlerBase<ClientNameChangedEvent>
{
    private readonly IReportingRepository _reportingRepository;

    public ClientNameChangedEventHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public override async Task ExecuteAsync(ClientNameChangedEvent theEvent)
    {
        await _reportingRepository.UpdateAsync<ClientReport>(new { Name = theEvent.ClientName }, new { Id = theEvent.AggregateId });
        await _reportingRepository.UpdateAsync<ClientDetailsReport>(new { theEvent.ClientName }, new { Id = theEvent.AggregateId });
    }
}