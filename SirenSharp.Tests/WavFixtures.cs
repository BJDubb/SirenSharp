using System.IO;
using NAudio.Wave;

namespace SirenSharp.Tests
{
    // Builds synthetic WAV files on disk at runtime so the suite ships no binary
    // fixtures. Covers the valid case (mono 16-bit PCM) plus the malformed inputs
    // SirenSharp has to cope with in the wild: stereo, non-PCM float, and a file
    // that is not a readable RIFF/WAVE at all.
    internal static class WavFixtures
    {
        public static string MonoPcm16(string path, double seconds = 1.0, int sampleRate = 22050, double freq = 440.0)
        {
            var format = new WaveFormat(sampleRate, 16, 1);
            using var writer = new WaveFileWriter(path, format);
            WriteSine(writer, sampleRate, 1, seconds, freq);
            return path;
        }

        public static string StereoPcm16(string path, double seconds = 1.0, int sampleRate = 22050, double freq = 440.0)
        {
            var format = new WaveFormat(sampleRate, 16, 2);
            using var writer = new WaveFileWriter(path, format);
            WriteSine(writer, sampleRate, 2, seconds, freq);
            return path;
        }

        public static string MonoFloat32(string path, double seconds = 1.0, int sampleRate = 22050, double freq = 440.0)
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            using var writer = new WaveFileWriter(path, format);
            WriteSine(writer, sampleRate, 1, seconds, freq);
            return path;
        }

        // A .wav file whose bytes are not a valid RIFF/WAVE container.
        public static string JunkHeader(string path)
        {
            var junk = new byte[1024];
            new Random(1234).NextBytes(junk);
            junk[0] = (byte)'J'; junk[1] = (byte)'U'; junk[2] = (byte)'N'; junk[3] = (byte)'K';
            File.WriteAllBytes(path, junk);
            return path;
        }

        private static void WriteSine(WaveFileWriter writer, int sampleRate, int channels, double seconds, double freq)
        {
            var total = (int)(sampleRate * seconds);
            var buffer = new float[channels];
            for (var i = 0; i < total; i++)
            {
                var sample = (float)(0.8 * Math.Sin(2 * Math.PI * freq * i / sampleRate));
                for (var c = 0; c < channels; c++) buffer[c] = sample;
                writer.WriteSamples(buffer, 0, channels);
            }
        }
    }

    // Per-test scratch directory that cleans itself up.
    internal sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sirensharp_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string File(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, true); } catch { }
        }
    }
}
