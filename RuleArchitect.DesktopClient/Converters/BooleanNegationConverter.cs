// In RuleArchitect.DesktopClient/Converters/BooleanNegationConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    public class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value; // Or false, or Binding.DoNothing, depending on desired behavior for non-bool values
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value; // Or throw new NotSupportedException();
        }
    }
}