namespace SirenSharp.Models
{
    public class WavFormatInfo
    {
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public int SampleRate { get; set; }
        public bool IsPcm { get; set; }
        public bool IsMono => Channels == 1;
        public bool Is16BitPcm => IsPcm && BitsPerSample == 16;

        public bool IsCompatible => IsMono && Is16BitPcm;

        public string GetIssuesSummary()
        {
            var issues = new List<string>();
            if (!IsPcm) issues.Add("not 16-bit PCM");
            if (Channels > 1) issues.Add($"stereo ({Channels} channels)");
            if (IsPcm && BitsPerSample != 16) issues.Add($"{BitsPerSample}-bit (need 16-bit)");
            return issues.Count == 0 ? string.Empty : string.Join(", ", issues);
        }
    }
}
