using Fohjin.DDD.Events.Account;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.EventHandlers;

public class ClosedAccountCreatedEventHandler : EventHandlerBase<ClosedAccountCreatedEvent>
{
    private readonly IReportingRepository _reportingRepository;

    public ClosedAccountCreatedEventHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public override async Task ExecuteAsync(ClosedAccountCreatedEvent theEvent)
    {
        var closedAccount = new ClosedAccountReport(theEvent.AccountId, theEvent.ClientId, theEvent.AccountName, theEvent.AccountNumber);
        var closedAccountDetails = new ClosedAccountDetailsReport(theEvent.AccountId, theEvent.ClientId, theEvent.AccountName, 0, theEvent.AccountNumber);

        await _reportingRepository.SaveAsync(closedAccount);
        await _reportingRepository.SaveAsync(closedAccountDetails);

        foreach (var ledger in theEvent.Ledgers)
        {
            var split = ledger.Value.Split('|');
            var amount = Convert.ToDecimal(split[0]);
            var account = split.Length > 1 ? split[1] : string.Empty;
            await _reportingRepository.SaveAsync(new LedgerReport(Guid.NewGuid(), theEvent.AccountId, GetDescription(ledger.Key, account), amount));
        }
    }

    private static string GetDescription(string transferType, string accountNumber)
    {
        if (transferType == "CreditMutation")
            return "Deposit";

        if (transferType == "DebitMutation")
            return "Withdrawal";

        if (transferType == "CreditTransfer")
            return string.Format("Transfer to {0}", accountNumber);

        if (transferType == "DebitTransfer")
            return string.Format("Transfer from {0}", accountNumber);

        if (transferType == "DebitTransferFailed")
            return string.Format("Transfer to {0} failed", accountNumber);

        throw new UnsupportedTransferTypeException(transferType);
    }
}