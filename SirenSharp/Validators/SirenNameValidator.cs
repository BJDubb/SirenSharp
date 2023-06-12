using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SirenSharp.Validators
{
    public class SirenNameValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string sirenName = (string)value;

            if (string.IsNullOrEmpty(sirenName) ) return new ValidationResult(false, "Name can't be empty.");

            if (sirenName.Contains(" ")) return new ValidationResult(false, "Name can't include spaces.");

            return ValidationResult.ValidResult;
        }
    }
}
