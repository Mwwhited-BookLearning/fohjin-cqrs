using Fohjin.DDD.ApiClient;
using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Common;

namespace Fohjin.DDD.BankApplication.Presenters;

public class ClientSearchFormPresenter(
    IClientSearchFormView clientSearchFormView,
    IClientDetailsPresenter clientDetailsPresenter,
    IPopupPresenter popupPresenter,
    FohjinApiClient apiClient,
    ISystemTimer systemTimer
        ) : Presenter<IClientSearchFormView>(clientSearchFormView), IClientSearchFormPresenter
{
    private readonly IClientSearchFormView _clientSearchFormView = clientSearchFormView;
    private readonly IPopupPresenter _popupPresenter = popupPresenter;
    private readonly IClientDetailsPresenter _clientDetailsPresenter = clientDetailsPresenter;
    private readonly FohjinApiClient _apiClient = apiClient;
    private readonly ISystemTimer _systemTimer = systemTimer;

    public void CreateNewClient()
    {
        _clientDetailsPresenter.SetClient(null);
        _clientDetailsPresenter.Display();
        _systemTimer.Trigger(LoadDataAsync, 2000);
    }

    public void OpenSelectedClient()
    {
        _popupPresenter.CatchPossibleException(() =>
        {
            var client = _clientSearchFormView.GetSelectedClient();
            _clientDetailsPresenter.SetClient(client);
            _clientDetailsPresenter.Display();
        });
    }

    public async void Display()
    {
        await LoadDataAsync();
        try
        {
            _clientSearchFormView.ShowDialog();
        }
        finally
        {
            _clientSearchFormView.Dispose();
        }
    }

    private async Task LoadDataAsync()
    {
        _clientSearchFormView.Clients = await _apiClient.GetClientsAsync();
    }
}