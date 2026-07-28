using Fohjin.DDD.Services.Models;

namespace Fohjin.DDD.Services;

public interface IReceiveMoneyTransfers
{
    Task Receive(MoneyTransfer moneyTransfer);
}