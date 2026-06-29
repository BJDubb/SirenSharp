namespace SirenSharp.Models
{
    /// <summary>
    /// A point-in-time bundle of everything useful for diagnosing a project or a failed
    /// build: app/environment info, the project being worked on, the latest preflight
    /// findings, and (if a build was attempted) the generation findings and AWC
    /// verifications. Feeds both the "export diagnostics" action and the failed-build report.
    /// </summary>
    public sealed class DiagnosticsSnapshot
    {
        public string AppVersion { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

        public string OperatingSystem { get; set; } = string.Empty;
        public string RuntimeVersion { get; set; } = string.Empty;
        public string AwcBackend { get; set; } = string.Empty;

        public string? ProjectName { get; set; }
        public string? DlcName { get; set; }
        public int SoundSetCount { get; set; }
        public int SoundCount { get; set; }

        public DiagnosticReport Preflight { get; set; } = new();

        /// <summary>Null when no build has been attempted for this snapshot.</summary>
        public DiagnosticReport? Generation { get; set; }

        public List<AwcVerificationResult> AwcVerifications { get; set; } = new();
    }
}
