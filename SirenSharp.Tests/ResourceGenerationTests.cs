using System.IO;
using SirenSharp.Models;
using SirenSharp.Services;
using Xunit;

namespace SirenSharp.Tests
{
    public class ResourceGenerationTests
    {
        private static ResourceGenerator BuildGenerator() =>
            new ResourceGenerator(
                new AwcGenerator(),
                new DataGenerator(),
                new WavSanitizer(new WavFormatAnalyzer()),
                new AwcVerifier());

        [Fact]
        public void GenerateResource_ProducesHealthyPack()
        {
            using var dir = new TempDir();

            // One already-compatible mono source and one stereo source that must be
            // downmixed during generation - exercises the full sanitize -> encode path.
            var wail = WavFixtures.MonoPcm16(dir.File("wail.wav"), seconds: 1.0, freq: 600);
            var yelp = WavFixtures.StereoPcm16(dir.File("yelp.wav"), seconds: 1.0, freq: 900);

            var soundSet = new SoundSet("lspd");
            soundSet.AddSound(new Sound(wail));
            soundSet.AddSound(new Sound(yelp));

            var options = new ResourceGenerationOptions
            {
                ResourceName = "test_sirens",
                DlcName = "policesirens",
                FolderPath = dir.Path,
                FxVersion = "cerulean",
                SoundSets = new List<SoundSet> { soundSet }
            };

            var result = BuildGenerator().GenerateResource(options, null);

            Assert.True(result.Success, string.Join(" | ", result.Errors));

            var resourceDir = Path.Combine(dir.Path, "test_sirens");
            Assert.True(File.Exists(Path.Combine(resourceDir, "fxmanifest.lua")));
            Assert.True(File.Exists(Path.Combine(resourceDir, "SIRENSHARP_NOTES.txt")));
            Assert.True(File.Exists(Path.Combine(resourceDir, "data", "policesirens.dat54.rel")));
            Assert.True(File.Exists(Path.Combine(resourceDir, "data", "policesirens.dat54.nametable")));

            var awcPath = Path.Combine(resourceDir, "dlc_policesirens", "lspd.awc");
            Assert.True(File.Exists(awcPath));

            // The raw/ working directory is cleaned up after generation.
            Assert.False(Directory.Exists(Path.Combine(resourceDir, "raw")));

            // Post-generation verification passed and the AWC clears the silent-file gate.
            Assert.All(result.AwcVerifications, v => Assert.True(v.IsHealthy, v.Summary));
            var awcSize = new FileInfo(awcPath).Length;
            Assert.True(awcSize > 2048, $"AWC was only {awcSize} bytes");

            var manifest = File.ReadAllText(Path.Combine(resourceDir, "fxmanifest.lua"));
            Assert.Contains("fx_version 'cerulean'", manifest);
            Assert.Contains("dlc_policesirens/lspd.awc", manifest);
            Assert.Contains("data_file \"AUDIO_WAVEPACK\" \"dlc_policesirens\"", manifest);
            Assert.Contains("data/policesirens.dat54.rel", manifest);

            var nametable = File.ReadAllText(Path.Combine(resourceDir, "data", "policesirens.dat54.nametable"));
            Assert.Contains("lspd_soundset\0", nametable);
            Assert.Contains("wail_s\0", nametable);
            Assert.Contains("yelp_s\0", nametable);
        }

        [Fact]
        public void GenerateResource_WithTester_EmitsRunnableTesterAndPrefilledConfig()
        {
            using var dir = new TempDir();
            var wail = WavFixtures.MonoPcm16(dir.File("wail.wav"));

            var soundSet = new SoundSet("lspd");
            soundSet.AddSound(new Sound(wail));

            var options = new ResourceGenerationOptions
            {
                ResourceName = "test_sirens",
                DlcName = "policesirens",
                FolderPath = dir.Path,
                GenerateInGameTester = true,
                SoundSets = new List<SoundSet> { soundSet }
            };

            var result = BuildGenerator().GenerateResource(options, null);
            Assert.True(result.Success, string.Join(" | ", result.Errors));

            var testerDir = Path.Combine(dir.Path, "sirensharp-audio-test");
            Assert.Equal(testerDir, result.TesterPath);

            // Template (RageUI + scripts) extracted from the embedded resource.
            Assert.True(File.Exists(Path.Combine(testerDir, "fxmanifest.lua")));
            Assert.True(File.Exists(Path.Combine(testerDir, "client.lua")));
            Assert.True(File.Exists(Path.Combine(testerDir, "RageUI", "src", "RageUI.lua")));

            // config.lua is generated from the project's names.
            var config = File.ReadAllText(Path.Combine(testerDir, "config.lua"));
            Assert.Contains("dlc = 'policesirens'", config);
            Assert.Contains("name = 'lspd'", config);
            Assert.Contains("'wail'", config);

            // RageUI's IsVisible requires a panels callback - omitting it crashes the menu.
            // Both menu blocks (main + soundset submenu) must supply one.
            var clientLua = File.ReadAllText(Path.Combine(testerDir, "client.lua"));
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(clientLua, @"end, function\(Panels\) end\)").Count);
        }
    }
}
