namespace SirenSharp.Models
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// A single structured finding produced anywhere in the pipeline - preflight,
    /// audio sanitize, AWC build, or post-build verification. Replaces the old
    /// flat <c>List&lt;string&gt;</c> error/warning lists so the UI (and a future
    /// diagnostics export) can group, filter, and act on findings rather than
    /// just print them.
    /// </summary>
    /// <param name="Severity">How serious the finding is.</param>
    /// <param name="Message">Human-readable description of what happened.</param>
    /// <param name="Code">Stable machine code (see <see cref="DiagnosticCodes"/>) for
    /// grouping/filtering and diagnostics export. Null for ad-hoc findings.</param>
    /// <param name="Target">What the finding is about, e.g. "lspd/wail" or a file path.</param>
    /// <param name="SuggestedAction">Optional actionable guidance for the user.</param>
    public sealed record Diagnostic(
        DiagnosticSeverity Severity,
        string Message,
        string? Code = null,
        string? Target = null,
        string? SuggestedAction = null)
    {
        /// <summary>Single-line form used by the results UI and test assertions.</summary>
        public override string ToString()
        {
            var head = string.IsNullOrEmpty(Target) ? Message : $"{Target}: {Message}";
            return string.IsNullOrEmpty(SuggestedAction) ? head : $"{head} ({SuggestedAction})";
        }
    }

    /// <summary>
    /// Stable diagnostic codes. Kept in one place so the UI and diagnostics export
    /// can reason about findings without string-matching messages.
    /// </summary>
    public static class DiagnosticCodes
    {
        // Preflight (pre-generation project validation)
        public const string ProjectEmpty = "PROJECT.EMPTY";
        public const string AwcDuplicateName = "AWC.DUPLICATE_NAME";
        public const string AwcInvalidName = "AWC.INVALID_NAME";
        public const string AwcNoSirens = "AWC.NO_SIRENS";
        public const string SirenDuplicateName = "SIREN.DUPLICATE_NAME";
        public const string SirenInvalidName = "SIREN.INVALID_NAME";
        public const string SirenMissingFile = "SIREN.MISSING_FILE";
        public const string AudioNeedsConversion = "AUDIO.NEEDS_CONVERSION";

        // Build pipeline
        public const string WavPrepareFailed = "AUDIO.PREPARE_FAILED";
        public const string WavAutoConverted = "AUDIO.AUTO_CONVERTED";
        public const string AwcBuildFailed = "AWC.BUILD_FAILED";
        public const string AwcEmpty = "AWC.EMPTY";
        public const string AwcUnhealthy = "AWC.UNHEALTHY";
        public const string TesterFailed = "TESTER.FAILED";
        public const string Unexpected = "GENERAL.UNEXPECTED";
    }
}
