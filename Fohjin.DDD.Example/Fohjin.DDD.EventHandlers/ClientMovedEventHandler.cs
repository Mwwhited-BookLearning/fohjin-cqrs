using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class ClientMovedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<ClientMovedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override async Task ExecuteAsync(ClientMovedEvent theEvent)
    {
        await _reportingRepository.UpdateAsync<ClientDetailsReport>(new { theEvent.Street, theEvent.StreetNumber, theEvent.PostalCode, theEvent.City }, new { Id = theEvent.AggregateId });
    }
}