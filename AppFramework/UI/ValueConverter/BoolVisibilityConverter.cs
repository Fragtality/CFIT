using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class BoolVisibilityConverter : IValueConverter
    {
        public virtual Visibility VisibleFalse { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool visibility && targetType == typeof(Visibility))
                return visibility ? Visibility.Visible : VisibleFalse;
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility && targetType == typeof(bool))
                return visibility == Visibility.Visible;
            else
                return value;
        }
    }
}
