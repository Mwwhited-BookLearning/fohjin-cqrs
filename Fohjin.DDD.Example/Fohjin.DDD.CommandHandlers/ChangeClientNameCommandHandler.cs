using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Client;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class ChangeClientNameCommandHandler(IDomainRepository<IDomainEvent> repository) : CommandHandlerBase<ChangeClientNameCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository = repository;

    public override async Task ExecuteAsync(ChangeClientNameCommand compensatingCommand)
    {
        var client = await _repository.GetByIdAsync<Client>(compensatingCommand.Id);
        client?.UpdateClientName(new ClientName(compensatingCommand.ClientName));
    }
}