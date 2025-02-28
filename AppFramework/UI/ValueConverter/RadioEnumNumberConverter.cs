using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class RadioEnumNumberConverter<TEnum> : IValueConverter
    {
        protected int Target { get; }
        protected Type EnumType { get; } = typeof(TEnum);

        public RadioEnumNumberConverter(TEnum target)
        {
            Target = System.Convert.ToInt32(target);
        }

        public RadioEnumNumberConverter(int target)
        {
            Target = target;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Enum @enum)
                {
                    if (Target == System.Convert.ToInt32(@enum))
                        return true;
                    else
                        return false;
                }
                else if (value is int number)
                {
                    if (Target == number)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
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
                if (targetType == typeof(TEnum))
                    return (TEnum)(object)Target;
                else if (targetType == typeof(int))
                    return Target;
                else
                    return 0;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}
