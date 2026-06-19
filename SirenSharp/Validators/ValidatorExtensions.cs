using System.Globalization;
using System.Windows.Controls;

namespace SirenSharp.Validators
{
    public static class ValidatorExtensions
    {
        public static ValidationResult ValidateValue(this ValidationRule rule, string? value)
        {
            return rule.Validate(value ?? string.Empty, CultureInfo.CurrentCulture);
        }
    }
}
