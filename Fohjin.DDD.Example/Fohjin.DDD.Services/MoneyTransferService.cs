using Fohjin.DDD.Bus;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Common;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;
using Fohjin.DDD.Services.Models;

namespace Fohjin.DDD.Services
{

    public class MoneyTransferService : ISendMoneyTransfer
    {
        private readonly IBus _bus;
        private readonly IReportingRepository _reportingRepository;
        private readonly IReceiveMoneyTransfers _receiveMoneyTransfers;
        private readonly IDictionary<int, Func<MoneyTransfer, Task>> _moneyTransferOptions;
        private readonly ISystemTimer _systemTimer;
        private readonly ISystemRandom _systemRandom;

        public MoneyTransferService(
            IBus bus,
            IReportingRepository reportingRepository,
            IReceiveMoneyTransfers receiveMoneyTransfers,
            ISystemTimer systemTimer,
            ISystemRandom systemRandom
            )
        {
            _bus = bus;
            _reportingRepository = reportingRepository;
            _receiveMoneyTransfers = receiveMoneyTransfers;
            _systemTimer = systemTimer;
            _systemRandom = systemRandom;

            _moneyTransferOptions = new Dictionary<int, Func<MoneyTransfer, Task>>
            {
                {0, MoneyTransferIsGoingToAnInternalAccountAsync},
                {1, MoneyTransferIsGoingToAnInternalAccountAsync},
                {2, MoneyTransferIsGoingToAnExternalAccountAsync},
                {3, MoneyTransferIsGoingToAnExternalAccountAsync},
                {4, MoneyTransferIsGoingToAnExternalNonExistingAccountAsync},
                {5, MoneyTransferIsGoingToAnInternalAccountAsync},
                {6, MoneyTransferIsGoingToAnInternalAccountAsync},
                {7, MoneyTransferIsGoingToAnExternalAccountAsync},
                {8, MoneyTransferIsGoingToAnExternalAccountAsync},
            };
        }

        public void Send(MoneyTransfer moneyTransfer)
        {
            _systemTimer.Trigger(() => DoSendAsync(moneyTransfer), @in: 5000);
        }

        private async Task DoSendAsync(MoneyTransfer moneyTransfer)
        {
            try
            {
                // I didn't want to introduce an actual external bank, so that's why you see this nice construct :)
                await _moneyTransferOptions[_systemRandom.Next(start: 0, end: 9)](moneyTransfer);
            }
            catch (Exception)
            {
                await CompensatingActionBecauseOfFailedMoneyTransferAsync(moneyTransfer);
            }
        }

        private async Task MoneyTransferIsGoingToAnInternalAccountAsync(MoneyTransfer moneyTransfer)
        {
            var account = (await _reportingRepository.GetByExampleAsync<AccountReport>(new { AccountNumber = moneyTransfer.TargetAccount })).First();
            _bus.Publish(new ReceiveMoneyTransferCommand(account.Id, moneyTransfer.Amount, moneyTransfer.SourceAccount));
            await _bus.CommitAsync();
        }

        private Task MoneyTransferIsGoingToAnExternalAccountAsync(MoneyTransfer moneyTransfer)
        {
            _receiveMoneyTransfers.Receive(moneyTransfer);
            return Task.CompletedTask;
        }

        private Task MoneyTransferIsGoingToAnExternalNonExistingAccountAsync(MoneyTransfer moneyTransfer)
        {
            _receiveMoneyTransfers.Receive(new MoneyTransfer(moneyTransfer.SourceAccount, moneyTransfer.TargetAccount?.Reverse().ToString(), moneyTransfer.Amount));
            return Task.CompletedTask;
        }

        private async Task CompensatingActionBecauseOfFailedMoneyTransferAsync(MoneyTransfer moneyTransfer)
        {
            var account = (await _reportingRepository.GetByExampleAsync<AccountReport>(new { AccountNumber = moneyTransfer.SourceAccount })).First();
            _bus.Publish(new MoneyTransferFailedCompensatingCommand(account.Id, moneyTransfer.Amount, moneyTransfer.TargetAccount));
        }
    }
}