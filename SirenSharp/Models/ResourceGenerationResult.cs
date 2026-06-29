namespace SirenSharp.Models
{
    public class ResourceGenerationResult
    {
        public bool Success { get; set; }
        public string ResourcePath { get; set; } = string.Empty;
        public string? TesterPath { get; set; }

        /// <summary>
        /// Structured findings from the whole build (audio prep, AWC build, verification).
        /// Replaces the old flat error/warning string lists.
        /// </summary>
        public DiagnosticReport Diagnostics { get; } = new();

        public List<AwcVerificationResult> AwcVerifications { get; set; } = new();
    }
}
