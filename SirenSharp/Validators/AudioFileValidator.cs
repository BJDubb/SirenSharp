using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using NAudio.FileFormats.Wav;
using NAudio.Wave;

namespace SirenSharp.Validators
{
    public class AudioFileValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string filepath = (string)value;

            //if (string.IsNullOrEmpty(filepath)) return ValidationResult.ValidResult;

            if (!File.Exists(filepath)) return new ValidationResult(false, "File does not exist");

            try
            {
                new WaveFileReader(filepath);
            }
            catch (FormatException e)
            { 
                return new ValidationResult(false, e.Message);
            }

            return ValidationResult.ValidResult;
        }
    }
}
