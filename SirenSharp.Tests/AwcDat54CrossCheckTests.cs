using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using CodeWalker.GameFiles;
using SirenSharp.Models;
using SirenSharp.Services;
using SirenSharp.Services.Backends;
using Xunit;

namespace SirenSharp.Tests
{
    /// <summary>
    /// The dat54 references each wave by a FileName hash; the game looks that hash up in the
    /// AWC. If the two sides derive the name differently (casing, suffixes) the lookup fails
    /// and the siren plays silent. These tests cross-check the actual generated dat54 XML
    /// against the actual built AWC, so a future divergence breaks the build, not a user's server.
    /// </summary>
    public class AwcDat54CrossCheckTests
    {
        [Fact]
        public void EveryDat54WaveReferenceResolvesToAnAwcStream()
        {
            using var dir = new TempDir();
            // Mixed case + multiple sirens - the exact shape that used to break.
            // Fixtures named to match each Sound's prepared file ({Name}.wav) - the backend is
            // fed directly here, without the sanitizer that would normally write those files.
            var wail = WavFixtures.MonoPcm16(dir.File("Wail.wav"), seconds: 1.0, freq: 500);
            var yelp = WavFixtures.MonoPcm16(dir.File("Yelp_02.wav"), seconds: 1.0, freq: 800);

            var soundSet = new SoundSet("police");
            soundSet.AddSound(new Sound(wail) { Name = "Wail" });
            soundSet.AddSound(new Sound(yelp) { Name = "Yelp_02" });
            var soundSets = new List<SoundSet> { soundSet };

            // AWC side: the hashes the game will find in the bank.
            var build = new CodeWalkerAwcBuildBackend(new AwcVerifier()).BuildAwc(soundSet, dir.Path);
            Assert.True(build.Success, build.Error?.Message);
            var awc = new AwcFile();
            awc.Load(build.Data, new RpfBinaryFileEntry { Name = "police.awc" });
            var awcHashes = awc.Streams.Select(s => s.Hash.Hash).ToHashSet();

            // dat54 side: the FileName the game will hash to look the wave up. Parse the real
            // generated XML rather than re-deriving the name.
            var datXml = new DataGenerator().GenerateDatXml(new DLC("policesirens"), soundSets);
            var fileNames = XDocument.Parse(datXml)
                .Descendants("Item")
                .Where(i => (string?)i.Attribute("type") == "SimpleSound")
                .Select(i => i.Element("FileName")?.Value)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .ToList();

            Assert.Equal(2, fileNames.Count);
            foreach (var name in fileNames)
            {
                var refHash = JenkHash.GenHash(name) & 0x1FFFFFFF;
                Assert.True(awcHashes.Contains(refHash),
                    $"dat54 references wave '{name}' (hash {refHash:X}) but no AWC stream has that hash");
            }
        }
    }
}
