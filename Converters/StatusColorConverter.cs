using System.Globalization;

namespace Prototype_Final_Project.Converters;

/// <summary>
/// Value converter mapping domain lifecycle status strings directly to visual branding colors.
/// OpenIdea -> Slate Gray (#64748B)
/// UnderReview -> Amber (#D97706)
/// InDevelopment -> Royal Blue (#2563EB)
/// Shipped -> Forest Green (#16A34A)
/// Architectural Tier: Presentation Tier (Section 6 of Architecture Specification).
/// </summary>
public class StatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? string.Empty;
        return status switch
        {
            "OpenIdea" => Color.FromArgb("#64748B"),      // Slate Gray
            "UnderReview" => Color.FromArgb("#D97706"),   // Amber
            "InDevelopment" => Color.FromArgb("#2563EB"), // Royal Blue
            "Shipped" => Color.FromArgb("#16A34A"),       // Forest Green
            _ => Color.FromArgb("#94A3B8")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
