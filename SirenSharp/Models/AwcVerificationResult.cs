namespace SirenSharp.Models
{
    public class AwcVerificationResult
    {
        public string SoundSetName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int StreamCount { get; set; }
        public List<AwcStreamVerification> Streams { get; set; } = new();
        public bool IsHealthy { get; set; }
        public string? ErrorMessage { get; set; }

        public string Summary =>
            IsHealthy
                ? $"{SoundSetName}: OK ({StreamCount} stream(s), {FileSizeBytes / 1024.0:F1} KB)"
                : $"{SoundSetName}: {ErrorMessage ?? "verification failed"}";
    }

    public class AwcStreamVerification
    {
        public string Name { get; set; } = string.Empty;
        public int DataBytes { get; set; }
        public int SampleRate { get; set; }
        public uint SampleCount { get; set; }
        public bool IsSilent { get; set; }
    }
}
