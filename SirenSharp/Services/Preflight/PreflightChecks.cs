using SirenSharp.Models;
using SirenSharp.Validators;

namespace SirenSharp.Services.Preflight
{
    /// <summary>
    /// Project-level structure rules: the project must contain soundsets and their
    /// names must be unique.
    /// </summary>
    public sealed class ProjectStructureCheck : IPreflightCheck
    {
        public IEnumerable<Diagnostic> Inspect(Project project)
        {
            if (project.SoundSets.Count == 0)
            {
                yield return new Diagnostic(
                    DiagnosticSeverity.Error,
                    "Project has no AWCs. Add at least one soundset before generating.",
                    DiagnosticCodes.ProjectEmpty);
                yield break;
            }

            var duplicateNames = project.SoundSets
                .GroupBy(s => s.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var name in duplicateNames)
            {
                yield return new Diagnostic(
                    DiagnosticSeverity.Error,
                    "AWCs cannot have the same name.",
                    DiagnosticCodes.AwcDuplicateName,
                    name);
            }

            // The game identifies wavepacks (AWCs) by the first 8 characters of the name only
            // (RAGE truncates them; see FiveM's PatchAudioWavePackOverlay 8-char match). Two AWCs
            // whose names match in the first 8 chars resolve to the same bank, so only one loads
            // and the other plays silent in-game - even though both names look distinct here.
            var prefixCollisions = project.SoundSets
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .GroupBy(s => s.Name.ToLowerInvariant().Substring(0, Math.Min(8, s.Name.Length)))
                .Where(g => g.Select(s => s.Name).Distinct().Count() > 1);

            foreach (var group in prefixCollisions)
            {
                var names = string.Join(", ", group.Select(s => s.Name).Distinct());
                yield return new Diagnostic(
                    DiagnosticSeverity.Warning,
                    $"AWCs {names} share the same first 8 characters ('{group.Key}'). The game uses only the first 8 characters to identify a wavepack, so these resolve to the same bank - only one will load and the others will be silent in-game.",
                    DiagnosticCodes.AwcNameCollision,
                    group.Key,
                    "Make the first 8 characters of each AWC name unique.");
            }
        }
    }

    /// <summary>
    /// Per-soundset rules: valid AWC name, no duplicate siren names, at least one siren.
    /// </summary>
    public sealed class SoundSetCheck : IPreflightCheck
    {
        public IEnumerable<Diagnostic> Inspect(Project project)
        {
            var nameValidator = new AwcNameValidator();

            foreach (var soundSet in project.SoundSets)
            {
                var nameResult = nameValidator.ValidateValue(soundSet.Name);
                if (!nameResult.IsValid)
                {
                    yield return new Diagnostic(
                        DiagnosticSeverity.Error,
                        nameResult.ErrorContent?.ToString() ?? "Invalid AWC name.",
                        DiagnosticCodes.AwcInvalidName,
                        soundSet.Name);
                }

                if (soundSet.Sounds.Count == 0)
                {
                    yield return new Diagnostic(
                        DiagnosticSeverity.Warning,
                        "AWC has no sirens - it will be skipped or produce an empty container.",
                        DiagnosticCodes.AwcNoSirens,
                        soundSet.Name,
                        "Add at least one siren or remove the AWC.");
                }

                var duplicateSirens = soundSet.Sounds
                    .GroupBy(s => s.Name)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var siren in duplicateSirens)
                {
                    yield return new Diagnostic(
                        DiagnosticSeverity.Error,
                        "Sirens in an AWC cannot have the same name.",
                        DiagnosticCodes.SirenDuplicateName,
                        $"{soundSet.Name}/{siren}");
                }
            }
        }
    }

    /// <summary>
    /// Per-siren rules: valid siren name, a WAV file that exists, and a heads-up when
    /// the WAV will be auto-converted during generation.
    /// </summary>
    public sealed class SirenAudioCheck : IPreflightCheck
    {
        public IEnumerable<Diagnostic> Inspect(Project project)
        {
            var nameValidator = new SirenNameValidator();

            foreach (var soundSet in project.SoundSets)
            {
                foreach (var sound in soundSet.Sounds)
                {
                    var target = $"{soundSet.Name}/{sound.Name}";

                    var nameResult = nameValidator.ValidateValue(sound.Name);
                    if (!nameResult.IsValid)
                    {
                        yield return new Diagnostic(
                            DiagnosticSeverity.Error,
                            nameResult.ErrorContent?.ToString() ?? "Invalid siren name.",
                            DiagnosticCodes.SirenInvalidName,
                            target);
                    }

                    if (string.IsNullOrWhiteSpace(sound.AudioPath) || !File.Exists(sound.AudioPath))
                    {
                        yield return new Diagnostic(
                            DiagnosticSeverity.Error,
                            "No WAV file selected or the file does not exist.",
                            DiagnosticCodes.SirenMissingFile,
                            target,
                            "Select a valid WAV file for this siren.");
                    }
                    else if (sound.FormatState == SoundFormatState.NeedsConversion)
                    {
                        var detail = string.IsNullOrEmpty(sound.FormatStatus) ? "not mono 16-bit PCM" : sound.FormatStatus;
                        yield return new Diagnostic(
                            DiagnosticSeverity.Warning,
                            $"WAV will be auto-converted during generation ({detail}).",
                            DiagnosticCodes.AudioNeedsConversion,
                            target,
                            "Use 'Fix Audio' to convert it now, or let generation convert it.");
                    }
                }
            }
        }
    }
}
