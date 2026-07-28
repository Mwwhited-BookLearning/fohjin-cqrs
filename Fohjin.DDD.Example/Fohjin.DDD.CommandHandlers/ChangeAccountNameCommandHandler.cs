using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Account;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class ChangeAccountNameCommandHandler : CommandHandlerBase<ChangeAccountNameCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository;

    public ChangeAccountNameCommandHandler(IDomainRepository<IDomainEvent> repository)
    {
        _repository = repository;
    }

    public override async Task ExecuteAsync(ChangeAccountNameCommand compensatingCommand)
    {
        var activeAccount = await _repository.GetByIdAsync<ActiveAccount>(compensatingCommand.Id);

        activeAccount?.ChangeAccountName(new AccountName(compensatingCommand.AccountName));
    }
}