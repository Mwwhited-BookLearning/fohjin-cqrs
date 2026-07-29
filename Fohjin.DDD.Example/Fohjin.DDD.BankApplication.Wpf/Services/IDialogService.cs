namespace Fohjin.DDD.BankApplication.Wpf.Services;

// WPF's equivalent of WinForms' IPopupPresenter (Fohjin.DDD.BankApplication.Core) - a shared
// error-display seam a ViewModel can be unit tested against without a real MessageBox.
public interface IDialogService
{
    void ShowError(string message);
}
