using CFIT.AppTools;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class StringColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string htmlColor)
            {
                if (targetType == typeof(System.Drawing.Color))
                    return System.Drawing.ColorTranslator.FromHtml(htmlColor);
                else if (targetType == typeof(System.Windows.Media.Color))
                    return System.Drawing.ColorTranslator.FromHtml(htmlColor).Convert();
                else if (targetType == typeof(System.Windows.Media.Brush) || targetType == typeof(System.Windows.Media.SolidColorBrush))
                    return new System.Windows.Media.SolidColorBrush(System.Drawing.ColorTranslator.FromHtml(htmlColor).Convert());
                else
                    return value;
            }
            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(string))
            {
                if (value is System.Drawing.Color drawingColor)
                    return System.Drawing.ColorTranslator.ToHtml(drawingColor);
                else if (value is System.Windows.Media.Color mediaColor)
                    return System.Drawing.ColorTranslator.ToHtml(mediaColor.Convert());
                else if (value is System.Windows.Media.SolidColorBrush brush)
                    return System.Drawing.ColorTranslator.ToHtml(brush.Color.Convert());
                else
                    return value;
            }
            else
                return value;
        }
    }
}
