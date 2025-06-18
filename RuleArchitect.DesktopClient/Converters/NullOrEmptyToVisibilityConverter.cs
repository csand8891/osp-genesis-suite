using System;
using System.Globalization;
using System.Windows; // Required for Visibility
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input value (ErrorMessage) is null or an empty string.
            // If it is, return Collapsed (hide the TextBlock).
            // If it's not null or empty, return Visible (show the TextBlock).
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for this scenario.
            throw new NotImplementedException();
        }
    }
}