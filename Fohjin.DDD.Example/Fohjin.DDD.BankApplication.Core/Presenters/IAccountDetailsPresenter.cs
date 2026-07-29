using Fohjin.DDD.ApiClient;

namespace Fohjin.DDD.BankApplication.Presenters;

public interface IAccountDetailsPresenter : IPresenter
{
    void SetAccount(AccountReport? accountReport);
}