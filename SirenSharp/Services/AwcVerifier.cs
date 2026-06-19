using CodeWalker.GameFiles;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public class AwcVerifier
    {
        private const int MinimumHealthyAwcBytes = 2048;

        public AwcVerificationResult Verify(string soundSetName, string awcFilePath)
        {
            var result = new AwcVerificationResult
            {
                SoundSetName = soundSetName,
                FilePath = awcFilePath
            };

            if (!File.Exists(awcFilePath))
            {
                result.ErrorMessage = "AWC file was not created.";
                return result;
            }

            var bytes = File.ReadAllBytes(awcFilePath);
            result.FileSizeBytes = bytes.Length;

            if (bytes.Length < MinimumHealthyAwcBytes)
            {
                result.ErrorMessage = $"AWC is only {bytes.Length} bytes - likely silent or broken. Re-check source WAV format (mono, 16-bit PCM).";
                return result;
            }

            try
            {
                var awc = new AwcFile();
                awc.Load(bytes, new RpfBinaryFileEntry { Name = Path.GetFileName(awcFilePath) });

                if (!string.IsNullOrEmpty(awc.ErrorMessage))
                {
                    result.ErrorMessage = awc.ErrorMessage;
                    return result;
                }

                result.StreamCount = awc.Streams?.Length ?? 0;

                if (result.StreamCount == 0)
                {
                    result.ErrorMessage = "AWC contains no audio streams.";
                    return result;
                }

                foreach (var stream in awc.Streams!)
                {
                    var dataBytes = stream.DataChunk?.Data?.Length ?? 0;
                    var sampleRate = stream.FormatChunk?.SamplesPerSecond ?? 0;
                    var sampleCount = stream.FormatChunk?.Samples ?? 0;

                    var streamResult = new AwcStreamVerification
                    {
                        Name = stream.Name ?? "unknown",
                        DataBytes = dataBytes,
                        SampleRate = sampleRate,
                        SampleCount = sampleCount,
                        IsSilent = dataBytes < 256 || IsMostlySilent(stream.DataChunk?.Data)
                    };

                    result.Streams.Add(streamResult);
                }

                if (result.Streams.All(s => s.IsSilent))
                {
                    result.ErrorMessage = "All streams appear silent or have no PCM data.";
                    return result;
                }

                if (result.Streams.Any(s => s.IsSilent))
                {
                    var silent = string.Join(", ", result.Streams.Where(s => s.IsSilent).Select(s => s.Name));
                    result.ErrorMessage = $"Some streams appear silent: {silent}";
                    return result;
                }

                result.IsHealthy = true;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static bool IsMostlySilent(byte[]? data)
        {
            if (data == null || data.Length < 2) return true;

            var sampleCount = Math.Min(data.Length / 2, 8000);
            var threshold = 0;
            var loud = 0;

            for (var i = 0; i < sampleCount * 2; i += 2)
            {
                var sample = BitConverter.ToInt16(data, i);
                if (Math.Abs(sample) > threshold) loud++;
            }

            return loud < sampleCount * 0.01;
        }
    }
}
