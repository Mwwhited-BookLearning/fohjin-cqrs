using Fohjin.DDD.BankApplication.Views;

namespace Fohjin.DDD.BankApplication.Presenters;

public class PopupPresenter(IPopupView popupView) : Presenter<IPopupView>(popupView), IPopupPresenter
{
    private readonly IPopupView _popupView = popupView;

    public void CatchPossibleException(System.Action action)
    {
        try
        {
            action();
        }
        catch (Exception Ex)
        {
            SetException(Ex);
            Display();
        }
    }

    private void SetException(Exception exception)
    {
        _popupView.Exception = exception.GetType().Name;
        _popupView.Message = exception.Message;
    }

    public void Display()
    {
        _popupView.ShowDialog();
    }
}