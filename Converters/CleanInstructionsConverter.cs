using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace FitnessApp.Converters
{
    /// <summary>
    /// Strips step-number prefixes (e.g., "Step:1", "Step 1:", "Step1:") from instructions
    /// and formats them into a clean continuous paragraph for card previews.
    /// </summary>
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

            // Remove all Step prefixes
            var cleaned = StepPrefixRegex.Replace(instructions, string.Empty);

            // Replace newlines with spaces to form a continuous paragraph preview in the catalog
            cleaned = cleaned.Replace("\r\n", " ").Replace("\n", " ");

            // Clean up any extra spacing
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            return cleaned;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
