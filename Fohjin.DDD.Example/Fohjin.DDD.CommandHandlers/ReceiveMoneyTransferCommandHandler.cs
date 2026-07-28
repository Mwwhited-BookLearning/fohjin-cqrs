using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers
{
    public class ReceiveMoneyTransferCommandHandler : CommandHandlerBase<ReceiveMoneyTransferCommand>
    {
        private readonly IDomainRepository<IDomainEvent> _repository;

        public ReceiveMoneyTransferCommandHandler(IDomainRepository<IDomainEvent> repository)
        {
            _repository = repository;
        }

        public override async Task ExecuteAsync(ReceiveMoneyTransferCommand compensatingCommand)
        {
            var activeAccount = await _repository.GetByIdAsync<ActiveAccount>(compensatingCommand.Id);

            activeAccount?.ReceiveTransferFrom(new AccountNumber(compensatingCommand.AccountNumber), new Amount(compensatingCommand.Amount));
        }
    }
}