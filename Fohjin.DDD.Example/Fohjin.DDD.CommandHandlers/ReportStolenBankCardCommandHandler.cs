using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Client;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers
{
    public class ReportStolenBankCardCommandHandler : CommandHandlerBase<ReportStolenBankCardCommand>
    {
        private readonly IDomainRepository<IDomainEvent> _repository;

        public ReportStolenBankCardCommandHandler(IDomainRepository<IDomainEvent> repository)
        {
            _repository = repository;
        }

        public override async Task ExecuteAsync(ReportStolenBankCardCommand cancelReportStolenBankCardCommand)
        {
            var client = await _repository.GetByIdAsync<Client>(cancelReportStolenBankCardCommand.Id);
            var bankCard = client?.GetBankCard(cancelReportStolenBankCardCommand.BankCardId);
            bankCard?.BankCardIsReportedStolen();
            if (client != null)
                _repository.Add(client);
        }
    }
}