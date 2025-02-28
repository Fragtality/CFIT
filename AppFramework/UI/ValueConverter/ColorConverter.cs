using CFIT.AppTools;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class DrawingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color color)
            {
                if (targetType == typeof(System.Windows.Media.Color))
                    return color.Convert();
                else if (targetType == typeof(System.Windows.Media.Brush) || targetType == typeof(System.Windows.Media.SolidColorBrush))
                    return new System.Windows.Media.SolidColorBrush(color.Convert());
                else
                    return value;
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(System.Drawing.Color))
            {
                if (value is System.Windows.Media.Color color)
                    return color.Convert();
                else if (value is System.Windows.Media.SolidColorBrush brush)
                    return brush.Color.Convert();
                else
                    return value;
            }
            else
                return value;
        }
    }

    public class MediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(System.Drawing.Color))
            {
                if (value is System.Windows.Media.Color color)
                    return color.Convert();
                else if (value is System.Windows.Media.SolidColorBrush brush)
                    return brush.Color.Convert();
                else
                    return value;
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color color)
            {
                if (targetType == typeof(System.Windows.Media.Color))
                    return color.Convert();
                else if (targetType == typeof(System.Windows.Media.Brush) || targetType == typeof(System.Windows.Media.SolidColorBrush))
                    return new System.Windows.Media.SolidColorBrush(color.Convert());
                else
                    return value;
            }
            else
                return value;
        }
    }
}
