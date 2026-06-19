using System.Globalization;
using System.IO;
using System.Windows.Controls;
using NAudio.Wave;
using SirenSharp.Services;

namespace SirenSharp.Validators
{
    public class AudioFileValidator : ValidationRule
    {
        private static readonly WavFormatAnalyzer Analyzer = new();

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var filepath = value as string;

            if (string.IsNullOrWhiteSpace(filepath))
                return new ValidationResult(false, "No audio file selected");

            if (!File.Exists(filepath))
                return new ValidationResult(false, "File does not exist");

            if (!Analyzer.TryAnalyze(filepath, out var info, out var error))
                return new ValidationResult(false, error ?? "Invalid WAV file");

            if (!info!.IsCompatible)
            {
                return new ValidationResult(false,
                    $"WAV must be mono 16-bit PCM ({info.GetIssuesSummary()}). Use Fix Audio or re-export from Audacity.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
