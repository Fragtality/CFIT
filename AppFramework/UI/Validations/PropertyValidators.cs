using CFIT.AppTools;
using System;
using System.ComponentModel.DataAnnotations;

namespace CFIT.AppFramework.UI.Validations
{
    public static class PropertyValidators
    {
        private static ValidationResult Validate(Func<bool> validation, string errorMessage = "Validation Error")
        {
            if (validation.Invoke())
                return ValidationResult.Success;
            else
                return new(errorMessage);
        }
#pragma warning disable
        public static ValidationResult ValidateString(string text, ValidationContext context)
#pragma warning restore
        {
            return Validate(() => { return !string.IsNullOrWhiteSpace(text); }, "Text can not be empty!");
        }
#pragma warning disable
        public static ValidationResult ValidateStringNumber(string text, ValidationContext context)
#pragma warning restore
        {
            return Validate(() => { return Conversion.IsNumber(text, out _); }, "Not a valid Number!");
        }
#pragma warning disable
        public static ValidationResult ValidateStringInteger(string text, ValidationContext context)
#pragma warning restore
        {
            return Validate(() => { return Conversion.IsNumberI(text, out _); }, "Not a valid Integer!");
        }
    }
}
