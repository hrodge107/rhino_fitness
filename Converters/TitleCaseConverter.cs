using System.Globalization;
using Microsoft.Maui.Controls;

namespace FitnessApp.Converters;

public class TitleCaseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            return culture.TextInfo.ToTitleCase(str.ToLower());
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
