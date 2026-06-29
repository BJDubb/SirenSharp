using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using SirenSharp.Models;
using SirenSharp.Services;
using SirenSharp.Services.Backends;
using Xunit;

namespace SirenSharp.Tests
{
    public class AwcBuildBackendTests
    {
        private static CodeWalkerAwcBuildBackend Backend() => new(new AwcVerifier());

        [Fact]
        public void BuildAwc_HashesStreamNamesLowercased()
        {
            using var dir = new TempDir();
            // Mixed-case siren name - the dat54 references it lower-cased, so the AWC must
            // hash the lower-cased name or the game can't find the wave (silent siren).
            var wav = WavFixtures.MonoPcm16(dir.File("police_wail.wav"), seconds: 1.0, freq: 600);

            var soundSet = new SoundSet("police");
            soundSet.AddSound(new Sound(wav) { Name = "Police_Wail" });

            var result = Backend().BuildAwc(soundSet, dir.Path);
            Assert.True(result.Success, result.Error?.Message);

            var awc = new AwcFile();
            awc.Load(result.Data, new RpfBinaryFileEntry { Name = "police.awc" });

            Assert.Null(awc.ErrorMessage);
            var stream = Assert.Single(awc.Streams);

            var expected = JenkHash.GenHash("police_wail") & 0x1FFFFFFF;
            Assert.Equal(expected, stream.Hash.Hash);
            Assert.True(stream.SamplesPerSecond > 0);
        }

        [Fact]
        public void BuildAwc_ProducesLoadableMultiStreamBank()
        {
            using var dir = new TempDir();
            var a = WavFixtures.MonoPcm16(dir.File("wail.wav"), seconds: 1.0, freq: 500);
            var b = WavFixtures.MonoPcm16(dir.File("yelp.wav"), seconds: 1.0, freq: 800);

            var soundSet = new SoundSet("lspd");
            soundSet.AddSound(new Sound(a));
            soundSet.AddSound(new Sound(b));

            var result = Backend().BuildAwc(soundSet, dir.Path);
            Assert.True(result.Success, result.Error?.Message);

            var awc = new AwcFile();
            awc.Load(result.Data, new RpfBinaryFileEntry { Name = "lspd.awc" });

            Assert.Null(awc.ErrorMessage);
            Assert.Equal(2, awc.Streams.Length);
            Assert.All(awc.Streams, s => Assert.NotNull(s.FormatChunk));
            Assert.All(awc.Streams, s => Assert.NotNull(s.DataChunk));
            // Streams stored hash-ordered.
            Assert.True(awc.Streams[0].Hash.Hash <= awc.Streams[1].Hash.Hash);
        }
    }
}
