using System;
using System.Globalization;
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    public class NullToBooleanConverter : IValueConverter
    {
        public bool FalseForNull { get; set; } = true; // True: null -> false. False: null -> true.

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            return FalseForNull ? !isNull : isNull;
        }

        public object ConvertBack(object value, Type target, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
