using Fohjin.DDD.ApiClient;

namespace Fohjin.DDD.BankApplication.Presenters;

public interface IClientDetailsPresenter : IPresenter
{
    void SetClient(ClientReport? clientReport);
}