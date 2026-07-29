using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Fohjin.DDD.BankApplication.Wpf.Services;

// ViewModel-first navigation (docs/patterns/wpf-architecture.md): Views never construct a
// ViewModel or another View directly - a ViewModel's command calls one of these methods, this
// service constructs the target ViewModel (resolving its own dependencies via DI) and sets
// CurrentViewModel, and MainWindow.xaml's DataTemplates (Resources/ViewModelTemplates.xaml)
// are what actually pick the matching View to render for whatever type CurrentViewModel is.
// INotifyPropertyChanged (via ObservableObject in the implementation) is how MainWindow's
// ContentControl finds out CurrentViewModel changed - no separate event needed.
public interface INavigationService : INotifyPropertyChanged
{
    ObservableObject? CurrentViewModel { get; }

    void ShowClientSearch();
    void ShowClientCreate();
    void ShowClientDetails(Guid clientId);
    void ShowAccountDetails(Guid accountId);
}
