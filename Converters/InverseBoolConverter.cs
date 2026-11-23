using System.Globalization;

namespace Prototype_Final_Project.Converters;

/// <summary>
/// Inverts a boolean value for XAML visibility bindings (e.g. IsClaimed -> IsUnclaimed).
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }
}
