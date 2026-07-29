using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class WithdrawalCashCommandHandler(IDomainRepository<IDomainEvent> repository) : CommandHandlerBase<WithdrawalCashCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository = repository;

    public override async Task ExecuteAsync(WithdrawalCashCommand compensatingCommand)
    {
        var activeAccount = await _repository.GetByIdAsync<ActiveAccount>(compensatingCommand.Id);

        activeAccount?.Withdrawal(new Amount(compensatingCommand.Amount));
    }
}