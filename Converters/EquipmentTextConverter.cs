using System.Globalization;
using Microsoft.Maui.Controls;

namespace FitnessApp.Converters;

/// <summary>
/// Formats non-empty equipment names for compact catalog display.
/// </summary>
public class EquipmentTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var equipment = text
            .Split(new[] { ',', ';', '/', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.ToUpperInvariant());

        return string.Join(", ", equipment);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
