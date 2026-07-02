using System.Globalization;
using Microsoft.Maui.Controls;

namespace FitnessApp.Converters;

/// <summary>
/// Returns <c>true</c> when the bound string value is non-null, non-empty, and
/// not whitespace. Used to hide optional Equipment badges on bodyweight
/// exercises where the source data carries <c>null</c> equipment.
/// </summary>
public class NullEmptyToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
