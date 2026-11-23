using System.Globalization;

namespace Prototype_Final_Project.Converters;

/// <summary>
/// Converts submission type domain values to formatted badge text.
/// 'NewAppConcept' -> 'New App', 'FeatureRequest' -> 'Feature Request'.
/// </summary>
public class SubmissionTypeBadgeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string subType = value?.ToString() ?? string.Empty;
        return subType switch
        {
            "NewAppConcept" => "New App",
            "FeatureRequest" => "Feature Request",
            _ => subType
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
