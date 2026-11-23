using System.Globalization;

namespace Prototype_Final_Project.Converters;

/// <summary>
/// Returns true if a bound string contains non-whitespace text.
/// Used for conditional XAML visibility triggers.
/// </summary>
public class IsNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
