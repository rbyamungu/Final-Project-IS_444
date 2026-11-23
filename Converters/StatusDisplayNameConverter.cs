using System.Globalization;

namespace Prototype_Final_Project.Converters;

/// <summary>
/// Converts raw enum-style database strings into human-friendly status badge labels.
/// e.g. 'OpenIdea' -> 'Open Idea', 'InDevelopment' -> 'In Development'.
/// </summary>
public class StatusDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? string.Empty;
        return status switch
        {
            "OpenIdea" => "Open Idea",
            "UnderReview" => "Under Review",
            "InDevelopment" => "In Development",
            "Shipped" => "Shipped",
            _ => status
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
