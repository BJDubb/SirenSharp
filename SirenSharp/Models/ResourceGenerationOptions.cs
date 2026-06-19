namespace SirenSharp.Models
{
    public class ResourceGenerationOptions
    {
        public string ResourceName { get; set; } = string.Empty;
        public string DlcName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string FxVersion { get; set; } = "cerulean";
        public bool GenerateInGameTester { get; set; }
        public List<SoundSet> SoundSets { get; set; } = new();
    }
}
