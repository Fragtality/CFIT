using System;
using System.Collections.Generic;
using System.Linq;

namespace CFIT.AppTools
{
    public static class Extensions
    {
        public static bool HasProperty(this object obj, string name)
        {
            return obj?.GetType()?.GetProperties()?.Any(p => p.Name == name) == true;
        }

        public static bool HasProperty<T>(this object obj, string name, out T value)
        {
            var query = obj?.GetType()?.GetProperties()?.Where(p => p.Name == name);
            value = (T)query?.FirstOrDefault()?.GetValue(obj);
            return query?.Any() == true;
        }

        public static bool IsPropertyType<Type>(this object obj, string name)
        {
            return obj?.GetType()?.GetProperties()?.Where(p => p.Name == name)?.FirstOrDefault()?.PropertyType == typeof(Type);
        }

        public static T GetPropertyValue<T>(this object obj, string name)
        {
            return (T)obj?.GetType()?.GetProperties()?.Where(p => p.Name == name)?.FirstOrDefault()?.GetValue(obj);
        }

        public static void SetPropertyValue<T>(this object obj, string name, T value)
        {
            obj?.GetType()?.GetProperties()?.Where(p => p.Name == name)?.FirstOrDefault()?.SetValue(obj, value);
        }

        public static T SafeIndex<T>(this T[] array, uint index, T defValue = default)
        {
            if (array != null && index >= 0 && index < array.Length)
                return array[index];
            return
                defValue;
        }

        public static bool IsArrayOf<T>(this Array array)
        {
            return array?.Length > 0 && (array.GetValue(0) is T || array.GetValue(0) is T[]);
        }

        public static T StepNumber<T>(T value, double step)
        {
            try
            {
                if (typeof(T) == typeof(int))
                {
                    int integer = (int)(object)value;
                    integer += (int)step;
                    value = (T)(object)integer;
                }
                else if (typeof(T) == typeof(long))
                {
                    long bigInt = (long)(object)value;
                    bigInt += (long)step;
                    value = (T)(object)bigInt;
                }
                else if (typeof(T) == typeof(float))
                {
                    float single = (float)(object)value;
                    single += (float)step;
                    value = (T)(object)single;
                }
                else if (typeof(T) == typeof(double))
                {
                    double number = (double)(object)value;
                    number += step;
                    value = (T)(object)number;
                }

                return value;
            }
            catch
            {
                return value;
            }
        }

        public static bool CompareRange<T>(T value, T[] range)
        {
            T min = range[0];
            T max = range[1];

            Comparer<T> comparer = Comparer<T>.Default;
            int cmpMin = comparer.Compare(value, min);
            int cmpMax = comparer.Compare(value, max);
            return (cmpMin == 0 || cmpMin > 0) && (cmpMax == 0 || cmpMax < 0);
        }

        public static System.Windows.Media.Color Convert(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color Convert(this System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
