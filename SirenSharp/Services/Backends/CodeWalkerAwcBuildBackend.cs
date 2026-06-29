using System.Xml;
using CodeWalker.GameFiles;
using SirenSharp.Models;

namespace SirenSharp.Services.Backends
{
    /// <summary>
    /// The production AWC backend. Generates AWC XML from a soundset and hands it to
    /// CodeWalker's <see cref="XmlMeta"/> importer to encode the binary container, then
    /// verifies the result. This is the only place that knows about CodeWalker; the rest
    /// of the pipeline talks to <see cref="IAwcBuildBackend"/>.
    /// </summary>
    public sealed class CodeWalkerAwcBuildBackend : IAwcBuildBackend
    {
        private readonly AwcGenerator awcGenerator;
        private readonly AwcVerifier awcVerifier;

        public CodeWalkerAwcBuildBackend(AwcGenerator awcGenerator, AwcVerifier awcVerifier)
        {
            this.awcGenerator = awcGenerator;
            this.awcVerifier = awcVerifier;
        }

        public string Name => "CodeWalker";

        public bool IsExperimental => false;

        public AwcBuildResult BuildAwc(SoundSet soundSet, string preparedWavDirectory)
        {
            var awcXml = awcGenerator.GenerateAwcXml(soundSet);
            var awcDoc = new XmlDocument();
            awcDoc.LoadXml(awcXml);

            byte[] data;
            try
            {
                data = XmlMeta.GetData(awcDoc, MetaFormat.Awc, preparedWavDirectory) ?? Array.Empty<byte>();
            }
            catch (WavImportException wex)
            {
                return AwcBuildResult.Fail(DescribeWavImportError(soundSet.Name, wex));
            }
            catch (Exception ex)
            {
                return AwcBuildResult.Fail(new Diagnostic(
                    DiagnosticSeverity.Error,
                    $"Error generating AWC: {ex.Message}",
                    DiagnosticCodes.AwcBuildFailed,
                    soundSet.Name));
            }

            if (data.Length == 0)
            {
                return AwcBuildResult.Fail(new Diagnostic(
                    DiagnosticSeverity.Error,
                    "AWC produced no data.",
                    DiagnosticCodes.AwcEmpty,
                    soundSet.Name));
            }

            return AwcBuildResult.Ok(data);
        }

        public AwcVerificationResult Verify(string soundSetName, string awcFilePath)
            => awcVerifier.Verify(soundSetName, awcFilePath);

        private static Diagnostic DescribeWavImportError(string soundSetName, WavImportException wex)
        {
            var fileLabel = string.IsNullOrEmpty(wex.FilePath) ? soundSetName : $"{soundSetName}/{wex.FilePath}";

            var guidance = wex.Reason switch
            {
                WavImportError.UnsupportedEncoding =>
                    "the WAV is not 16-bit PCM. Use 'Fix Audio' or re-export from Audacity as 16-bit PCM.",
                WavImportError.UnsupportedChannelCount =>
                    $"the WAV is not mono ({wex.Channels?.ToString() ?? "multi"}-channel). Use 'Fix Audio' to downmix to mono.",
                WavImportError.NoAudioData =>
                    "the WAV contains no audio samples. Re-export a valid clip.",
                WavImportError.FileReadError =>
                    "the WAV file could not be read. Make sure it still exists and isn't open in another program.",
                _ => wex.Message,
            };

            return new Diagnostic(
                DiagnosticSeverity.Error,
                $"Could not build AWC: {guidance}",
                DiagnosticCodes.AwcBuildFailed,
                fileLabel);
        }
    }
}
