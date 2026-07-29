using CommunityToolkit.Mvvm.ComponentModel;

namespace Fohjin.DDD.BankApplication.Wpf.ViewModels;

// WPF's equivalent of the try/catch-and-surface-the-message shape every WinForms Presenter
// method and Vue composable action already follows (IPopupPresenter.CatchPossibleExceptionAsync;
// each composable's own try/catch - see docs/patterns/winforms-architecture.md and
// vue-architecture.md). Centralized here rather than repeated per [RelayCommand] method.
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string? errorMessage;

    protected async Task RunSafelyAsync(Func<Task> action)
    {
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
