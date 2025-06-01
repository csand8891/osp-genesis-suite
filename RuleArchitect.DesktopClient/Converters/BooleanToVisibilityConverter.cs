using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            if (value is bool b)
            {
                boolValue = b;
            }

            // Invert visibility if "invert" or "reverse" parameter is passed
            bool invert = false;
            if (parameter is string paramString && (paramString.ToLower() == "invert" || paramString.ToLower() == "reverse"))
            {
                invert = true;
            }

            if (invert)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = false;
                if (parameter is string paramString && (paramString.ToLower() == "invert" || paramString.ToLower() == "reverse"))
                {
                    invert = true;
                }

                if (invert)
                {
                    return visibility == Visibility.Collapsed;
                }
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}