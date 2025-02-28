using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;

namespace CFIT.AppFramework.UI.Validations
{
    public static class BaseRule
    {
        public static ValidationResult Validate(Func<bool> validation, string errorMessage = "Validation Error")
        {
            if (validation.Invoke())
                return ValidationResult.ValidResult;
            else
                return new(false, errorMessage);
        }
    }

    public class ValidationRuleNull : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() => { return value != null; }, "Value can not be null!");
        }
    }

    public class ValidationRuleString : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() => { return value is string text && !string.IsNullOrWhiteSpace(text); }, "Text can not be empty!");
        }
    }

    public class ValidationRuleStringNumber : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() => { return value is string text && Conversion.IsNumber(text, out _); }, "Not a valid Number!");
        }
    }

    public class ValidationRuleStringInteger : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() => { return value is string text && Conversion.IsNumberI(text, out _); }, "Not a valid Integer!");
        }
    }

    public class ValidationRuleTextNumber : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() =>
            {
                return value is string text && (Conversion.IsNumber(text) || !string.IsNullOrWhiteSpace(text)) ;
            }
            , "Not a valid String or Number!");
        }
    }

    public class ValidationRuleRange<T>(T min, T max) : ValidationRule
    {
        public T MinValue { get; } = min;
        public T MaxValue { get; } = max;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            bool CheckRange()
            {
                if (value == null)
                    return false;
                if (value is string text)
                {
                    if (string.IsNullOrWhiteSpace(text) || !Conversion.IsNumber(text, out double numValue))
                        return false;

                    if (typeof(double) == typeof(T))
                        value = (T)(object)numValue;
                    else if (typeof(int) == typeof(T) && int.TryParse(text, out int @int))
                        value = (T)(object)@int;
                    else if (typeof(float) == typeof(T))
                        value = (T)(object)(float)numValue;
                    else if (typeof(long) == typeof(T))
                        value = (T)(object)(long)numValue;
                    else
                        return false;
                }

                Comparer<T> comparer = Comparer<T>.Default;
                int cmpMin = comparer.Compare((T)value, MinValue);
                int cmpMax = comparer.Compare((T)value, MaxValue);
                return (cmpMin == 0 || cmpMin > 0) && (cmpMax == 0 || cmpMax < 0);
            }
            return BaseRule.Validate(CheckRange, "Value is not in Range!");
        }
    }
}
