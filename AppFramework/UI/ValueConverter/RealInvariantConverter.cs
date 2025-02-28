using CFIT.AppTools;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class RealInvariantConverter : IValueConverter
    {
        public string DefaultValue { get; set; } = null;

        public RealInvariantConverter()
        {

        }

        public RealInvariantConverter(string defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (targetType == typeof(string) && value.GetType() == typeof(double))
                    return Conversion.ToString((double)value);
                else if (targetType == typeof(string) && value.GetType() == typeof(float))
                    return Conversion.ToString((float)value);
                else if (targetType == typeof(string))
                    return value.ToString();
                else
                    return value;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (DefaultValue != null && string.IsNullOrEmpty(value as string))
                    value = DefaultValue;

                if (targetType == typeof(double) && Conversion.IsNumber(value as string, out double @double))
                    return @double;
                else if (targetType == typeof(float) && Conversion.IsNumberF(value as string, out float @float))
                    return @float;
                else if (targetType == typeof(int) && int.TryParse(value as string, out int @int))
                    return @int;
                else
                    return value;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
