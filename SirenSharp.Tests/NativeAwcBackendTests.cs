using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using SirenSharp.Models;
using SirenSharp.Services;
using SirenSharp.Services.Backends;
using SirenSharp.Services.Backends.Native;
using Xunit;

namespace SirenSharp.Tests
{
    /// <summary>
    /// The native writer must produce a bank the game (here: CodeWalker's reader, which
    /// round-trips real banks) reads identically to the proven CodeWalker-built bank.
    /// We compare decoded content, not raw bytes, because the chunk sort isn't stable.
    /// </summary>
    public class NativeAwcBackendTests
    {
        private static AwcFile Load(byte[] bytes, string name)
        {
            var awc = new AwcFile();
            awc.Load(bytes, new RpfBinaryFileEntry { Name = name });
            Assert.Null(awc.ErrorMessage);
            return awc;
        }

        [Fact]
        public void NativeOutput_MatchesCodeWalkerOutput_StreamForStream()
        {
            using var dir = new TempDir();
            // Long enough to force a peak chunk (>8192 samples) plus a short clip and mixed case.
            var wail = WavFixtures.MonoPcm16(dir.File("wail.wav"), seconds: 1.5, freq: 550);
            var yelp = WavFixtures.MonoPcm16(dir.File("yelp.wav"), seconds: 1.0, freq: 900);
            var horn = WavFixtures.MonoPcm16(dir.File("horn.wav"), seconds: 0.1, freq: 300);

            var soundSet = new SoundSet("police");
            soundSet.AddSound(new Sound(wail) { Name = "Wail" });
            soundSet.AddSound(new Sound(yelp) { Name = "yelp_02" });
            soundSet.AddSound(new Sound(horn) { Name = "AirHorn" });

            var native = new NativeAwcBuildBackend(new AwcVerifier()).BuildAwc(soundSet, dir.Path);
            var codewalker = new CodeWalkerAwcBuildBackend(new AwcVerifier()).BuildAwc(soundSet, dir.Path);
            Assert.True(native.Success, native.Error?.Message);
            Assert.True(codewalker.Success, codewalker.Error?.Message);

            var na = Load(native.Data, "police.awc");
            var cw = Load(codewalker.Data, "police.awc");

            var nStreams = na.Streams.OrderBy(s => s.Hash.Hash).ToArray();
            var cStreams = cw.Streams.OrderBy(s => s.Hash.Hash).ToArray();
            Assert.Equal(cStreams.Length, nStreams.Length);

            for (int i = 0; i < cStreams.Length; i++)
            {
                var n = nStreams[i];
                var c = cStreams[i];
                Assert.Equal(c.Hash.Hash, n.Hash.Hash);
                Assert.Equal(c.FormatChunk.Samples, n.FormatChunk.Samples);
                Assert.Equal(c.FormatChunk.SamplesPerSecond, n.FormatChunk.SamplesPerSecond);
                Assert.Equal(c.FormatChunk.Codec, n.FormatChunk.Codec);
                Assert.Equal(c.FormatChunk.Headroom, n.FormatChunk.Headroom);
                Assert.Equal(c.FormatChunk.PeakVal, n.FormatChunk.PeakVal);
                Assert.Equal(c.DataChunk.Data, n.DataChunk.Data);
                Assert.Equal(c.PeakChunk?.Data, n.PeakChunk?.Data);
            }
        }

        [Fact]
        public void NativeOutput_VerifiesHealthy()
        {
            using var dir = new TempDir();
            var wail = WavFixtures.MonoPcm16(dir.File("wail.wav"), seconds: 1.5, freq: 550);
            var soundSet = new SoundSet("police");
            soundSet.AddSound(new Sound(wail) { Name = "Wail" });

            var native = new NativeAwcBuildBackend(new AwcVerifier()).BuildAwc(soundSet, dir.Path);
            Assert.True(native.Success, native.Error?.Message);

            var awcPath = dir.File("police.awc");
            File.WriteAllBytes(awcPath, native.Data);
            var verification = new AwcVerifier().Verify("police", awcPath);
            Assert.True(verification.IsHealthy, verification.Summary);
        }
    }
}
