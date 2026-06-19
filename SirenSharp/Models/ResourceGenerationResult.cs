namespace SirenSharp.Models
{
    public class ResourceGenerationResult
    {
        public bool Success { get; set; }
        public string ResourcePath { get; set; } = string.Empty;
        public string? TesterPath { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<AwcVerificationResult> AwcVerifications { get; set; } = new();
    }
}
