using SirenSharp.Models;
using SirenSharp.Services.Backends;

namespace SirenSharp.Services
{
    /// <summary>
    /// Builds the GTA audio artifacts shared by every export target: sanitizes source
    /// WAVs, encodes each soundset's AWC via the active <see cref="IAwcBuildBackend"/>,
    /// verifies the result, and writes the dat54 metadata pair. Knows nothing about
    /// FiveM packaging - exporters layer that on top.
    /// </summary>
    public sealed class AudioPackBuilder
    {
        private readonly WavSanitizer wavSanitizer;
        private readonly DataGenerator dataGenerator;

        public AudioPackBuilder(WavSanitizer wavSanitizer, DataGenerator dataGenerator)
        {
            this.wavSanitizer = wavSanitizer;
            this.dataGenerator = dataGenerator;
        }

        /// <summary>
        /// Builds AWC files into <paramref name="awcOutputDir"/> and the dat54 pair into
        /// <paramref name="dataDir"/>, using <paramref name="rawDir"/> as a scratch area for
        /// sanitized WAVs. Findings are appended to <paramref name="result"/>. Returns false
        /// (and stops early) on the first error.
        /// </summary>
        public bool Build(
            DLC dlc,
            string rawDir,
            string awcOutputDir,
            string dataDir,
            IAwcBuildBackend backend,
            ResourceGenerationResult result,
            IProgress<string>? progress = null)
        {
            result.AwcBackendName = backend.IsExperimental ? $"{backend.Name} (experimental)" : backend.Name;

            foreach (var soundSet in dlc.SoundSets)
            {
                progress?.Report($"Preparing audio for '{soundSet.Name}'...");
                var awcDir = Directory.CreateDirectory(Path.Combine(rawDir, soundSet.Name));

                foreach (var sound in soundSet.Sounds)
                {
                    var destPath = Path.Combine(awcDir.FullName, sound.PreparedFileName);
                    var sanitize = wavSanitizer.Sanitize(
                        sound.AudioPath, destPath, sound.TrimStartSeconds, sound.TrimEndSeconds);

                    if (!sanitize.Success)
                    {
                        result.Diagnostics.AddError(
                            $"Failed to prepare WAV: {sanitize.Error}",
                            DiagnosticCodes.WavPrepareFailed,
                            $"{soundSet.Name}/{sound.Name}");
                        return false;
                    }

                    if (sanitize.WasConverted)
                    {
                        result.Diagnostics.AddWarning(
                            $"auto-converted ({string.Join(", ", sanitize.Changes)})",
                            DiagnosticCodes.WavAutoConverted,
                            $"{soundSet.Name}/{sound.Name}");
                    }
                }

                progress?.Report($"Building AWC '{soundSet.Name}' ({soundSet.Sounds.Count} sirens)...");
                var build = backend.BuildAwc(soundSet, awcDir.FullName);
                if (!build.Success)
                {
                    result.Diagnostics.Add(build.Error!);
                    return false;
                }

                var awcOutputPath = Path.Combine(awcOutputDir, soundSet.Name + ".awc");
                File.WriteAllBytes(awcOutputPath, build.Data);

                var verification = backend.Verify(soundSet.Name, awcOutputPath);
                result.AwcVerifications.Add(verification);

                if (!verification.IsHealthy)
                {
                    result.Diagnostics.AddError(
                        verification.ErrorMessage ?? "verification failed",
                        DiagnosticCodes.AwcUnhealthy,
                        soundSet.Name);
                    return false;
                }
            }

            progress?.Report("Writing dat54 metadata and manifest...");
            var nametableText = dataGenerator.GenerateNametable(dlc.SoundSets);
            var datData = dataGenerator.GenerateDatData(dlc, dlc.SoundSets);

            File.WriteAllText(Path.Combine(dataDir, $"{dlc.Name}.dat54.nametable"), nametableText);
            File.WriteAllBytes(Path.Combine(dataDir, $"{dlc.Name}.dat54.rel"), datData);

            return true;
        }
    }
}
