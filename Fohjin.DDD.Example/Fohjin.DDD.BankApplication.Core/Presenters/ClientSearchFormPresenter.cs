using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Common;
using Fohjin.DDD.Reporting;
using Fohjin.DDD.Reporting.Dtos;

namespace Fohjin.DDD.BankApplication.Presenters;

public class ClientSearchFormPresenter(
    IClientSearchFormView clientSearchFormView,
    IClientDetailsPresenter clientDetailsPresenter,
    IPopupPresenter popupPresenter,
    IReportingRepository reportingRepository,
    ISystemTimer systemTimer
        ) : Presenter<IClientSearchFormView>(clientSearchFormView), IClientSearchFormPresenter
{
    private readonly IClientSearchFormView _clientSearchFormView = clientSearchFormView;
    private readonly IPopupPresenter _popupPresenter = popupPresenter;
    private readonly IClientDetailsPresenter _clientDetailsPresenter = clientDetailsPresenter;
    private readonly IReportingRepository _reportingRepository = reportingRepository;
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
        _clientSearchFormView.Clients = await _reportingRepository.GetByExampleAsync<ClientReport>(null);
    }
}