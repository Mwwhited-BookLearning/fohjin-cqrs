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

    // HTTP calls (Phase 7, docs/11-migration-plan.md) are real async I/O, unlike the fire-and-
    // forget in-process bus.Publish()/CommitAsync() pairs this used to wrap - a sync
    // CatchPossibleException(Action) around an async lambda would return before the awaited
    // call inside it even runs, so its exceptions would never reach this try/catch at all.
    public async Task CatchPossibleExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetException(ex);
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