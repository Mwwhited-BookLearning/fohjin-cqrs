using Fohjin.DDD.Commands;
using Fohjin.DDD.Domain.Client;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.CommandHandlers;

public class CancelBankCardCommandHandler(IDomainRepository<IDomainEvent> repository) : CommandHandlerBase<CancelBankCardCommand>
{
    private readonly IDomainRepository<IDomainEvent> _repository = repository;

    public override async Task ExecuteAsync(CancelBankCardCommand cancelReportStolenBankCardCommand)
    {
        var client = await _repository.GetByIdAsync<Client>(cancelReportStolenBankCardCommand.Id);
        var bankCard = client?.GetBankCard(cancelReportStolenBankCardCommand.BankCardId);
        bankCard?.ClientCancelsBankCard();
        if (client != null)
            _repository.Add(client);
    }
}