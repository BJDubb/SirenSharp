using NAudio.Vorbis;
using NAudio.Wave;

namespace SirenSharp.Services
{
    /// <summary>
    /// Opens any supported source audio file as a <see cref="WaveStream"/> for reading,
    /// picking the right NAudio reader per extension (WAV/MP3/AIFF/WMA, and Vorbis for OGG).
    /// This is the single place that decides how a file is decoded, so the analyzer, the
    /// sanitizer, and the Sound metadata all accept the same formats.
    /// </summary>
    public static class AudioReaderFactory
    {
        /// <summary>Extensions accepted for import (lower-case, with leading dot).</summary>
        public static readonly string[] SupportedExtensions = { ".wav", ".mp3", ".ogg", ".aiff", ".aif", ".wma" };

        public static bool IsSupported(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return Array.IndexOf(SupportedExtensions, ext) >= 0;
        }

        /// <summary>Open-file dialog filter string covering all supported formats.</summary>
        public static string FileDialogFilter =>
            "Audio Files (*.wav;*.mp3;*.ogg;*.aiff;*.wma)|*.wav;*.mp3;*.ogg;*.aiff;*.aif;*.wma|All Files (*.*)|*.*";

        /// <summary>
        /// Opens the file with its true decoded format (NOT normalised to float), so the
        /// analyzer can tell a clean 16-bit PCM WAV from one that needs converting.
        /// </summary>
        public static WaveStream Open(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".wav" => new WaveFileReader(path),
                ".ogg" => new VorbisWaveReader(path),
                ".mp3" => new Mp3FileReader(path),
                ".aiff" or ".aif" => new AiffFileReader(path),
                ".wma" => new MediaFoundationReader(path),
                // Don't guess at unknown containers (the file dialog has an "All Files" option).
                _ => throw new NotSupportedException($"Unsupported audio format '{ext}'."),
            };
        }
    }
}
