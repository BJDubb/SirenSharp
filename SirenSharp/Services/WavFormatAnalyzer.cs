using NAudio.Wave;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public class WavFormatAnalyzer
    {
        public WavFormatInfo? Analyze(string filePath)
        {
            return TryAnalyze(filePath, out var info, out _) ? info : null;
        }

        public bool TryAnalyze(string filePath, out WavFormatInfo? info, out string? error)
        {
            info = null;
            error = null;

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                error = "File does not exist.";
                return false;
            }

            try
            {
                using var reader = new WaveFileReader(filePath);
                var format = reader.WaveFormat;
                info = new WavFormatInfo
                {
                    Channels = format.Channels,
                    BitsPerSample = format.BitsPerSample,
                    SampleRate = format.SampleRate,
                    IsPcm = format.Encoding == WaveFormatEncoding.Pcm
                };
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
