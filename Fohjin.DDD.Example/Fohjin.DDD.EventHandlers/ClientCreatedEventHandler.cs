using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class ClientCreatedEventHandler(IReportingRepository reportingRepository) : EventHandlerBase<ClientCreatedEvent>
{
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public override async Task ExecuteAsync(ClientCreatedEvent theEvent)
    {
        var client = new ClientReport(theEvent.ClientId, theEvent.ClientName);
        var clientDetails = new ClientDetailsReport(theEvent.ClientId, theEvent.ClientName, theEvent.Street, theEvent.StreetNumber, theEvent.PostalCode, theEvent.City, theEvent.PhoneNumber);
        await _reportingRepository.SaveAsync(client);
        await _reportingRepository.SaveAsync(clientDetails);
    }
}