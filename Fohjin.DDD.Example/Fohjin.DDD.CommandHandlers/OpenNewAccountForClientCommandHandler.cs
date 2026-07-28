using Fohjin.DDD.Commands;
using Fohjin.DDD.Common;
using Fohjin.DDD.Domain.Client;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class OpenNewAccountForClientCommandHandler(
    IDomainRepository<IDomainEvent> repository,
    ISystemHash systemHash
        ) : CommandHandlerBase<OpenNewAccountForClientCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository = repository;
    private readonly ISystemHash _systemHash = systemHash;

    public override async Task ExecuteAsync(OpenNewAccountForClientCommand compensatingCommand)
    {
        var client = await _repository.GetByIdAsync<Client>(compensatingCommand.Id);
        var activeAccount = client?.CreateNewAccount(
            compensatingCommand.AccountName,
            _systemHash.Hash(compensatingCommand.AccountName)
            );

        if (activeAccount != null)
            _repository.Add(activeAccount);
    }
}