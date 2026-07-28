using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Client;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers
{
    public class AssignNewBankCardCommandHandler : CommandHandlerBase<AssignNewBankCardCommand>
    {
        private readonly IDomainRepository<IDomainEvent> _repository;

        public AssignNewBankCardCommandHandler(IDomainRepository<IDomainEvent> repository)
        {
            _repository = repository;
        }

        public override async Task ExecuteAsync(AssignNewBankCardCommand assignNewCancelReportStolenBankCardCommand)
        {
            var client = await _repository.GetByIdAsync<Client>(assignNewCancelReportStolenBankCardCommand.Id);
            client?.AssignNewBankCardForAccount(assignNewCancelReportStolenBankCardCommand.AccountId);
            if (client != null)
                _repository.Add(client);
        }
    }
}