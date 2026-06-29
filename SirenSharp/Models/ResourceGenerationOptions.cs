namespace SirenSharp.Models
{
    public class ResourceGenerationOptions
    {
        public string ResourceName { get; set; } = string.Empty;
        public string DlcName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string FxVersion { get; set; } = "cerulean";
        public bool GenerateInGameTester { get; set; }

        /// <summary>Use the experimental from-scratch native AWC writer instead of CodeWalker.</summary>
        public bool UseNativeAwcBackend { get; set; }

        public List<SoundSet> SoundSets { get; set; } = new();
    }
}
