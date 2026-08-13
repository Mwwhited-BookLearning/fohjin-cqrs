using Fohjin.DDD.Bus;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Services.Models;

namespace Fohjin.DDD.Services;


public class MoneyReceiveService(IBus bus, IReportingRepository reportingRepository) : IReceiveMoneyTransfers
{
    private readonly IBus _bus = bus;
    private readonly IReportingRepository _reportingRepository = reportingRepository;

    public Task Receive(MoneyTransfer moneyTransfer) =>
        MoneyTransferIsGoingToAnInternalAccountAsync(moneyTransfer);

    private Task MoneyTransferIsGoingToAnInternalAccountAsync(MoneyTransfer moneyTransfer)
    {
        try
        {
            // Sync .First(), not .FirstAsync() - see MoneyTransferService's own comment on the
            // same pattern: EF Core's async LINQ operators need an IAsyncQueryProvider, which
            // a real EF context has but a mocked List<T>.AsQueryable() doesn't.
            var account = _reportingRepository.Query<AccountReport>().First(x => x.AccountNumber == moneyTransfer.TargetAccount);
            _bus.Publish(new ReceiveMoneyTransferCommand(account.Id, moneyTransfer.Amount, moneyTransfer.SourceAccount));
        }
        catch (Exception)
        {
            RequestedAccountDoesNotExist(moneyTransfer);
        }

        return Task.CompletedTask;
    }

    private static void RequestedAccountDoesNotExist(MoneyTransfer moneyTransfer)
    {
        throw new UnknownAccountException(string.Format("The requested account '{0}' is not managed by this bank", moneyTransfer.TargetAccount));
    }
}