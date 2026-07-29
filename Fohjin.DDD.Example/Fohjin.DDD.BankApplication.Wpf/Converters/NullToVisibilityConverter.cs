using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fohjin.DDD.BankApplication.Wpf.Converters;

// Shows an element only when the bound value is non-null/non-empty - used for the shared
// ErrorMessage property every ViewModel exposes (ViewModelBase).
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is string s ? !string.IsNullOrEmpty(s) : value is not null) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
