using System.Text.Json;

namespace SirenSharp.Services
{
    public class AppSettings
    {
        public string? CodeWalkerPath { get; set; }
        public List<string> RecentProjects { get; set; } = new();
        public string DefaultFxVersion { get; set; } = "cerulean";
        public bool UseDarkTheme { get; set; } = true;
        public bool UseNativeAwcBackend { get; set; } = false;
    }

    public class AppSettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SirenSharp", "settings.json");

        public AppSettings Settings { get; private set; }

        public AppSettingsService()
        {
            Settings = Load();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* use defaults */ }

            return new AppSettings();
        }
    }
}
