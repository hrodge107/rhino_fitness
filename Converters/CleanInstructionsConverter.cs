using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace FitnessApp.Converters
{
    /// <summary>Strips step prefixes and merges newlines for previews.</summary>
    public class CleanInstructionsConverter : IValueConverter
    {
        private static readonly Regex StepPrefixRegex = new(
            @"Step\s*:?\s*\d+\s*:?\s*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string instructions || string.IsNullOrWhiteSpace(instructions))
            {
                return string.Empty;
            }

            // Nuke step prefixes
            var cleaned = StepPrefixRegex.Replace(instructions, string.Empty);

            // Make it one paragraph
            cleaned = cleaned.Replace("\r\n", " ").Replace("\n", " ");

            // Collapse whitespace
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            return cleaned;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
