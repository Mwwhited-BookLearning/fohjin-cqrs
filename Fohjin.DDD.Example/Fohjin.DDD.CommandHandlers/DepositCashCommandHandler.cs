using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class DepositCashCommandHandler : CommandHandlerBase<DepositCashCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository;

    public DepositCashCommandHandler(IDomainRepository<IDomainEvent> repository)
    {
        _repository = repository;
    }

    public override async Task ExecuteAsync(DepositCashCommand compensatingCommand)
    {
        var activeAccount = await _repository.GetByIdAsync<ActiveAccount>(compensatingCommand.Id);

        activeAccount?.Deposit(new Amount(compensatingCommand.Amount));
    }
}