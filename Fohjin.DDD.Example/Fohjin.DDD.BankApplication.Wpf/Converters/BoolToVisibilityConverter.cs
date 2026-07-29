using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fohjin.DDD.BankApplication.Wpf.Converters;

// XAML data-binding has no built-in bool->Visibility conversion (unlike Vue's v-if, which just
// works against any truthy value) - IValueConverter is the WPF mechanism for this, registered
// once in Resources/Theme.xaml rather than per-binding.
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (Invert) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
