// File: RuleArchitect.DesktopClient/Converters/TrueFalseToCustomStringConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    public class TrueFalseToCustomStringConverter : IValueConverter
    {
        public string TrueString { get; set; } = "True";
        public string FalseString { get; set; } = "False";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueString : FalseString;
            }
            // Fallback or default if 'parameter' is the source and value is null or not bool
            if (parameter is string defaultString)
            {
                return defaultString;
            }
            return FalseString; // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}