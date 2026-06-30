using SirenSharp.Models;

namespace SirenSharp.Services.Backends.Native
{
    /// <summary>
    /// Experimental AWC backend that writes the binary itself (see <see cref="NativeAwcWriter"/>)
    /// and verifies it with <see cref="NativeAwcValidator"/> - no CodeWalker dependency on
    /// either the encode or the verify path. Marked experimental pending wider in-game use.
    /// </summary>
    public sealed class NativeAwcBuildBackend : IAwcBuildBackend
    {
        public string Name => "Native";

        public bool IsExperimental => true;

        public AwcBuildResult BuildAwc(SoundSet soundSet, string preparedWavDirectory)
        {
            try
            {
                var waves = new List<NativeAwcWriter.Wave>();
                foreach (var sound in soundSet.Sounds)
                {
                    var wavPath = Path.Combine(preparedWavDirectory, sound.PreparedFileName);
                    var (rate, pcm) = ReadMonoPcm16(File.ReadAllBytes(wavPath), sound.Name);
                    waves.Add(new NativeAwcWriter.Wave
                    {
                        Name = sound.Name,
                        SampleRate = rate,
                        Pcm = pcm,
                    });
                }

                var bytes = NativeAwcWriter.Write(waves);
                if (bytes.Length == 0)
                {
                    return AwcBuildResult.Fail(new Diagnostic(
                        DiagnosticSeverity.Error, "AWC produced no data.",
                        DiagnosticCodes.AwcEmpty, soundSet.Name));
                }

                return AwcBuildResult.Ok(bytes);
            }
            catch (Exception ex)
            {
                return AwcBuildResult.Fail(new Diagnostic(
                    DiagnosticSeverity.Error, $"Error generating AWC: {ex.Message}",
                    DiagnosticCodes.AwcBuildFailed, soundSet.Name));
            }
        }

        public AwcVerificationResult Verify(string soundSetName, string awcFilePath)
            => NativeAwcValidator.Verify(soundSetName, awcFilePath);

        // Minimal RIFF/WAVE reader for the sanitized mono 16-bit PCM the pipeline produces.
        private static (ushort sampleRate, byte[] pcm) ReadMonoPcm16(byte[] wav, string name)
        {
            const uint RIFF = 0x46464952, WAVE = 0x45564157, FMT = 0x20746D66, DATA = 0x61746164;

            if (wav.Length < 12 || BitConverter.ToUInt32(wav, 0) != RIFF || BitConverter.ToUInt32(wav, 8) != WAVE)
                throw new InvalidDataException($"{name}: not a valid RIFF/WAVE file.");

            ushort rate = 0;
            short channels = 0, bits = 0, codec = 0;
            byte[]? pcm = null;

            var p = 12;
            while (p + 8 <= wav.Length)
            {
                var id = BitConverter.ToUInt32(wav, p);
                var len = BitConverter.ToInt32(wav, p + 4);
                var body = p + 8;
                if (len < 0 || body + len > wav.Length) len = wav.Length - body;

                if (id == FMT && len >= 16)
                {
                    codec = BitConverter.ToInt16(wav, body);
                    channels = BitConverter.ToInt16(wav, body + 2);
                    rate = (ushort)BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToInt16(wav, body + 14);
                }
                else if (id == DATA)
                {
                    pcm = new byte[len];
                    Array.Copy(wav, body, pcm, 0, len);
                }

                p = body + len + (len & 1);
            }

            if (codec != 1 || channels != 1 || bits != 16)
                throw new InvalidDataException($"{name}: expected mono 16-bit PCM (got codec {codec}, {channels}ch, {bits}-bit).");
            if (pcm == null || pcm.Length == 0)
                throw new InvalidDataException($"{name}: WAV has no audio data.");

            return (rate, pcm);
        }
    }
}
