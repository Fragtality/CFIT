using System;
using System.Globalization;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class SafeEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, str);
                }
                catch
                {
                    return Binding.DoNothing;
                }
            }

            return Binding.DoNothing;
        }
    }
}
