using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RuleArchitect.DesktopClient.Converters
{
    public class RequirementTypeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a RequirementType string to a Visibility value.
        /// </summary>
        /// <param name="value">The RequirementType string from the binding source (RequirementViewModel.RequirementType).</param>
        /// <param name="targetType">The type of the binding target property (Visibility).</param>
        /// <param name="parameter">The string representation of the RequirementType that should result in Visibility.Visible.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Visibility.Visible if the value matches the parameter; otherwise, Visibility.Collapsed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currentRequirementType && parameter is string targetRequirementType)
            {
                return string.Equals(currentRequirementType, targetRequirementType, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed; // Default to collapsed if types are incorrect
        }

        /// <summary>
        /// Converts a Visibility value back to a RequirementType string. Not typically used.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}