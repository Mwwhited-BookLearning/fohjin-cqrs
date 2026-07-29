using System.Globalization;
using System.Windows.Data;

namespace Fohjin.DDD.BankApplication.Wpf.Converters;

// For IsEnabled="{Binding Submitting, Converter={StaticResource InverseBooleanConverter}}" -
// bool->bool, distinct from BoolToVisibilityConverter (bool->Visibility) since IsEnabled needs a
// plain bool, not a Visibility enum value.
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}
