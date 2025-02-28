using CFIT.AppTools;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class TextNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string text)
                {
                    if (Conversion.IsNumber(text, out double doubleValue))
                        return Conversion.ToString(doubleValue);
                    else if (Conversion.IsNumberF(text, out float floatValue))
                        return Conversion.ToString(floatValue);
                    else
                        return text;
                }
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
            return Convert(value, targetType, parameter, culture);
        }
    }
}
