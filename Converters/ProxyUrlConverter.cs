using System.Globalization;
using Microsoft.Maui.Controls;

namespace FitnessApp.Converters
{
    public class ProxyUrlConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string url && !string.IsNullOrWhiteSpace(url))
            {
                // If it's a static.exercisedb.dev URL, rewrite it through wsrv.nl proxy (which handles User-Agent and serves static/WebP format easily)
                if (url.Contains("exercisedb.dev"))
                {
                    return $"https://wsrv.nl/?url={Uri.EscapeDataString(url)}&n=-1";
                }
                return url;
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
