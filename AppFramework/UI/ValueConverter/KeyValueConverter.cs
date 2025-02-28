using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CFIT.AppFramework.UI.ValueConverter
{
    public class KeyValueStringConverter<K, V> : IValueConverter
    {
        public Func<K, string> KeyTransformator { get; }
        public Func<V, string> ValueTransformator { get; }
        public Func<KeyValuePair<K, V>, string> PairTransformator { get; }

        public KeyValueStringConverter(Func<KeyValuePair<K, V>, string> pairFunc = null, Func<K, string> keyFunc = null, Func<V, string> valueFunc = null)
        {
            PairTransformator = pairFunc;
            KeyTransformator = keyFunc;
            ValueTransformator = valueFunc;

            KeyTransformator ??= (k) => k?.ToString() ?? "";
            ValueTransformator ??= (v) => v?.ToString() ?? "";
            PairTransformator ??= (kv) => { return $"{KeyTransformator.Invoke(kv.Key)} = {ValueTransformator.Invoke(kv.Value)}"; };
        }

        public string Convert(KeyValuePair<K, V> pair)
        {
            return Convert(pair, typeof(string), null, null) as string;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (targetType == typeof(string) && value is KeyValuePair<K, V> kv)
                    return PairTransformator.Invoke(kv);

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
            return value;
        }
    }
}
