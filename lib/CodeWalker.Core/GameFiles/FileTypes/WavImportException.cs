using System;

namespace CodeWalker.GameFiles
{
    // NOTE: SirenSharp local addition to vendored CodeWalker.Core.
    // Provides a structured, consumer-friendly error for WAV -> AWC import
    // failures (see AwcFile.ParseWavFile / ReadXml) so callers can react to a
    // specific Reason instead of parsing a generic Exception message.

    public enum WavImportError
    {
        Unknown = 0,

        /// <summary>The WAV file could not be read from disk.</summary>
        FileReadError,

        /// <summary>The WAV is not PCM-encoded (format code != 1).</summary>
        UnsupportedEncoding,

        /// <summary>The WAV is not mono (channel count != 1).</summary>
        UnsupportedChannelCount,

        /// <summary>The WAV contains no audio samples.</summary>
        NoAudioData,
    }

    public class WavImportException : Exception
    {
        /// <summary>Categorised reason for the failure.</summary>
        public WavImportError Reason { get; }

        /// <summary>Source file that failed, when known.</summary>
        public string FilePath { get; }

        /// <summary>Channel count of the source WAV, when relevant.</summary>
        public int? Channels { get; }

        /// <summary>WAV format code of the source, when relevant.</summary>
        public int? FormatCode { get; }

        public WavImportException(
            WavImportError reason,
            string message,
            string filePath = null,
            int? channels = null,
            int? formatCode = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            Reason = reason;
            FilePath = filePath;
            Channels = channels;
            FormatCode = formatCode;
        }

        /// <summary>
        /// Returns a copy of this exception with the file path attached,
        /// preserving the original reason and details. Used to enrich an
        /// exception thrown deeper in the pipeline once the file name is known.
        /// </summary>
        public WavImportException WithFile(string filePath)
        {
            return new WavImportException(Reason, Message, filePath ?? FilePath, Channels, FormatCode, InnerException);
        }
    }
}
