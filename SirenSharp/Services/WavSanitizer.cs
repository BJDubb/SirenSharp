using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SirenSharp.Services
{
    public class WavSanitizeResult
    {
        public bool Success { get; set; }
        public bool WasConverted { get; set; }
        public string? Error { get; set; }
        public List<string> Changes { get; } = new();
    }

    public class WavSanitizer
    {
        private readonly WavFormatAnalyzer formatAnalyzer;

        public WavSanitizer(WavFormatAnalyzer formatAnalyzer)
        {
            this.formatAnalyzer = formatAnalyzer;
        }

        public WavSanitizeResult Sanitize(string inputPath, string outputPath)
        {
            var result = new WavSanitizeResult();

            if (!formatAnalyzer.TryAnalyze(inputPath, out var before, out var analyzeError))
            {
                result.Error = analyzeError;
                return result;
            }

            try
            {
                var targetFormat = new WaveFormat(before!.SampleRate, 16, 1);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                using var reader = new AudioFileReader(inputPath);
                ISampleProvider samples = reader;

                if (reader.WaveFormat.Channels > 1)
                {
                    samples = new StereoToMonoSampleProvider(samples)
                    {
                        LeftVolume = 0.5f,
                        RightVolume = 0.5f
                    };
                    result.Changes.Add("downmixed stereo to mono");
                }

                if (reader.WaveFormat.BitsPerSample != 16 || reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    result.Changes.Add($"converted to 16-bit PCM");
                }

                if (!before.IsCompatible)
                {
                    result.WasConverted = true;
                }

                using var writer = new WaveFileWriter(outputPath, targetFormat);
                var buffer = new float[targetFormat.SampleRate * targetFormat.Channels];
                int read;
                while ((read = samples.Read(buffer, 0, buffer.Length)) > 0)
                {
                    writer.WriteSamples(buffer, 0, read);
                }

                result.Success = true;
                if (result.Changes.Count == 0)
                {
                    result.Changes.Add("rewrote clean WAV (fmt + data only)");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }
    }
}
