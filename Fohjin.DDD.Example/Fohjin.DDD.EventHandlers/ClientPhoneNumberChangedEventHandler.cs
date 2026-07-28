using Fohjin.DDD.Events.Client;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers
{
    public class ClientPhoneNumberChangedEventHandler : EventHandlerBase<ClientPhoneNumberChangedEvent>
    {
        private readonly IReportingRepository _reportingRepository;

        public ClientPhoneNumberChangedEventHandler(IReportingRepository reportingRepository)
        {
            _reportingRepository = reportingRepository;
        }

        public override async Task ExecuteAsync(ClientPhoneNumberChangedEvent theEvent)
        {
            await _reportingRepository.UpdateAsync<ClientDetailsReport>(new { theEvent.PhoneNumber }, new { Id = theEvent.AggregateId });
        }
    }
}