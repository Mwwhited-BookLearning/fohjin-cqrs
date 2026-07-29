using Fohjin.DDD.ApiClient;

namespace Fohjin.DDD.BankApplication.Views;

public interface IClientSearchFormView : IView
{
    IEnumerable<ClientReport>? Clients { get; set; }
    ClientReport? GetSelectedClient();
    event Action? OnCreateNewClient;
    event Action? OnOpenSelectedClient;
}