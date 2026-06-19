using System.Linq;
using System.Xml.Linq;
using CodeWalker.GameFiles;
using SirenSharp.Models;
using SirenSharp.Services;
using Xunit;

namespace SirenSharp.Tests
{
    public class DataGeneratorTests
    {
        private readonly DataGenerator generator = new();

        private static SoundSet MakeSet(string name, params string[] sounds)
        {
            var set = new SoundSet(name);
            foreach (var s in sounds)
            {
                set.AddSound(new Sound { Name = s });
            }
            return set;
        }

        [Fact]
        public void Nametable_UsesSoundsetAndSuffixNaming()
        {
            var sets = new List<SoundSet> { MakeSet("lspd", "wail", "yelp") };

            var nametable = generator.GenerateNametable(sets);

            Assert.Contains("wail_s\0", nametable);
            Assert.Contains("yelp_s\0", nametable);
            Assert.Contains("lspd_soundset\0", nametable);
        }

        [Fact]
        public void Nametable_LowercasesNames()
        {
            var sets = new List<SoundSet> { MakeSet("LSPD", "Wail") };

            var nametable = generator.GenerateNametable(sets);

            Assert.Contains("wail_s\0", nametable);
            Assert.Contains("lspd_soundset\0", nametable);
            Assert.DoesNotContain("Wail", nametable);
        }

        [Fact]
        public void DatXml_EmitsSoundsetSimpleAndDirectionalNaming()
        {
            var dlc = new DLC("policesirens");
            var sets = new List<SoundSet> { MakeSet("lspd", "wail") };

            var xml = generator.GenerateDatXml(dlc, sets);
            var doc = XDocument.Parse(xml);

            var names = doc.Descendants("Name").Select(e => e.Value).ToList();
            Assert.Contains("lspd_soundset", names);
            Assert.Contains("wail_s", names);
            Assert.Contains("wail_d", names);

            Assert.Contains("wail", doc.Descendants("ScriptName").Select(e => e.Value));
            Assert.Contains("wail_d", doc.Descendants("ChildSound").Select(e => e.Value));
            Assert.Contains("dlc_policesirens/lspd", doc.Descendants("ContainerName").Select(e => e.Value));
        }

        [Fact]
        public void DatXml_ContainerPathsUseUppercase()
        {
            var dlc = new DLC("policesirens");
            var sets = new List<SoundSet> { MakeSet("lspd", "wail") };

            var xml = generator.GenerateDatXml(dlc, sets);
            var doc = XDocument.Parse(xml);

            var containerPaths = doc.Descendants("ContainerPaths").Elements("Item").Select(e => e.Value);
            Assert.Contains(@"DLC_POLICESIRENS\LSPD", containerPaths);
        }

        [Fact]
        public void DatXml_ScriptNamesOrderedByJenkHash()
        {
            var dlc = new DLC("policesirens");
            var soundNames = new[] { "yelp", "wail", "airhorn", "manual", "phaser" };
            var sets = new List<SoundSet> { MakeSet("lspd", soundNames) };

            var xml = generator.GenerateDatXml(dlc, sets);
            var doc = XDocument.Parse(xml);

            // ScriptName only appears inside the SoundSets block, which the generator
            // sorts by Jenkins hash of the (lowercased) name.
            var actual = doc.Descendants("ScriptName").Select(e => e.Value).ToList();
            var expected = soundNames.OrderBy(n => JenkHash.GenHash(n.ToLower())).ToList();

            Assert.Equal(expected, actual);
        }
    }
}
