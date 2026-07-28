using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class MoneyTransferFailedCompensatingCommandHandler(IDomainRepository<IDomainEvent> repository) : CommandHandlerBase<MoneyTransferFailedCompensatingCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository = repository;

    public override async Task ExecuteAsync(MoneyTransferFailedCompensatingCommand compensatingCommand)
    {
        var activeAccount = await _repository.GetByIdAsync<ActiveAccount>(compensatingCommand.Id);

        activeAccount?.PreviousTransferFailed(new AccountNumber(compensatingCommand.AccountNumber), new Amount(compensatingCommand.Amount));
    }
}