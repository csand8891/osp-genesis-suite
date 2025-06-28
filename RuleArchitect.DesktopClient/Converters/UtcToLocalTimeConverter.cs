// In RuleArchitect.DesktopClient/Converters/UtcToLocalTimeConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    /// <summary>
    /// Converts a DateTime value from UTC to the local time zone for display.
    /// </summary>
    public class UtcToLocalTimeConverter : IValueConverter
    {
        /// <summary>
        /// Converts a UTC DateTime to a local DateTime.
        /// </summary>
        /// <param name="value">The DateTime value from the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted local DateTime object.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime utcDate)
            {
                // Ensure the kind is UTC before converting, or assume it is if unspecified.
                if (utcDate.Kind == DateTimeKind.Unspecified)
                {
                    return DateTime.SpecifyKind(utcDate, DateTimeKind.Utc).ToLocalTime();
                }
                return utcDate.ToLocalTime();
            }
            return value;
        }

        /// <summary>
        /// Converts a local DateTime back to a UTC DateTime. This is not typically needed for display-only scenarios.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime localDate)
            {
                // Ensure the kind is Local before converting
                if (localDate.Kind == DateTimeKind.Unspecified)
                {
                    return DateTime.SpecifyKind(localDate, DateTimeKind.Local).ToUniversalTime();
                }
                return localDate.ToUniversalTime();
            }
            return value;
        }
    }
}
