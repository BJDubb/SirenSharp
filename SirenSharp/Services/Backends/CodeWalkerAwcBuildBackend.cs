using CodeWalker.GameFiles;
using SirenSharp.Models;

namespace SirenSharp.Services.Backends
{
    /// <summary>
    /// Builds AWC binaries by constructing CodeWalker's <see cref="AwcFile"/> object model
    /// directly and calling <see cref="AwcFile.Save"/> - the same binary serializer that
    /// round-trips real game files. This deliberately avoids the XML-meta conversion path,
    /// which is brittle and was the source of malformed packs.
    ///
    /// Stream names are hashed lower-cased so the wave hash in the AWC matches the
    /// lower-cased FileName the dat54 references; a case mismatch there makes the game
    /// fail to find the wave and the siren plays silent.
    /// </summary>
    public sealed class CodeWalkerAwcBuildBackend : IAwcBuildBackend
    {
        private readonly AwcVerifier awcVerifier;

        public CodeWalkerAwcBuildBackend(AwcVerifier awcVerifier)
        {
            this.awcVerifier = awcVerifier;
        }

        public string Name => "CodeWalker";

        public bool IsExperimental => false;

        public AwcBuildResult BuildAwc(SoundSet soundSet, string preparedWavDirectory)
        {
            try
            {
                var awc = new AwcFile
                {
                    Magic = 0x54414441,
                    Version = 1,
                    MultiChannelFlag = false,
                    ChunkIndicesFlag = true,
                };

                var streams = new List<AwcStream>();
                foreach (var sound in soundSet.Sounds)
                {
                    var wavPath = Path.Combine(preparedWavDirectory, sound.FileName);
                    var wav = File.ReadAllBytes(wavPath);

                    var stream = new AwcStream(awc)
                    {
                        Hash = JenkHash.GenHash(sound.Name.ToLowerInvariant()),
                    };

                    // Pre-build the format chunk; ParseWavFile fills sample rate + data and
                    // validates PCM/mono. Peak is set non-null so the 24-byte format layout
                    // (with peak value) is used; BuildPeakChunks fills the actual peak.
                    var format = new AwcFormatChunk(new AwcChunkInfo { Type = AwcChunkType.format })
                    {
                        Codec = AwcCodecType.PCM,
                        Headroom = -200,
                        LoopPoint = 0,
                        PlayBegin = 0,
                        PlayEnd = 0,
                        LoopBegin = 0,
                        LoopEnd = 0,
                        Peak = 0,
                    };
                    var data = new AwcDataChunk(new AwcChunkInfo { Type = AwcChunkType.data });

                    stream.Chunks = new AwcChunk[] { format, data };
                    stream.ExpandChunks();
                    stream.ParseWavFile(wav);
                    format.Samples = (uint)((data.Data?.Length ?? 0) / 2); // 16-bit mono PCM

                    streams.Add(stream);
                }

                // Game looks streams up by hash; keep them hash-ordered like real banks.
                streams.Sort((a, b) => a.Hash.Hash.CompareTo(b.Hash.Hash));
                awc.Streams = streams.ToArray();
                awc.StreamCount = streams.Count;

                awc.BuildPeakChunks();
                awc.BuildChunkIndices();
                awc.BuildStreamInfos();

                var bytes = awc.Save();
                if (bytes == null || bytes.Length == 0)
                {
                    return AwcBuildResult.Fail(new Diagnostic(
                        DiagnosticSeverity.Error, "AWC produced no data.",
                        DiagnosticCodes.AwcEmpty, soundSet.Name));
                }

                return AwcBuildResult.Ok(bytes);
            }
            catch (WavImportException wex)
            {
                return AwcBuildResult.Fail(DescribeWavImportError(soundSet.Name, wex));
            }
            catch (Exception ex)
            {
                return AwcBuildResult.Fail(new Diagnostic(
                    DiagnosticSeverity.Error, $"Error generating AWC: {ex.Message}",
                    DiagnosticCodes.AwcBuildFailed, soundSet.Name));
            }
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
