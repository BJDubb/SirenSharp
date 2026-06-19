using SirenSharp.Services;
using Xunit;

namespace SirenSharp.Tests
{
    public class WavFormatAnalyzerTests
    {
        private readonly WavFormatAnalyzer analyzer = new();

        [Fact]
        public void MonoPcm16_IsCompatible()
        {
            using var dir = new TempDir();
            var path = WavFixtures.MonoPcm16(dir.File("mono.wav"));

            Assert.True(analyzer.TryAnalyze(path, out var info, out var error));
            Assert.Null(error);
            Assert.NotNull(info);
            Assert.True(info!.IsMono);
            Assert.True(info.Is16BitPcm);
            Assert.True(info.IsCompatible);
        }

        [Fact]
        public void Stereo_ReadsButIsNotCompatible()
        {
            using var dir = new TempDir();
            var path = WavFixtures.StereoPcm16(dir.File("stereo.wav"));

            Assert.True(analyzer.TryAnalyze(path, out var info, out _));
            Assert.False(info!.IsMono);
            Assert.False(info.IsCompatible);
            Assert.Contains("stereo", info.GetIssuesSummary());
        }

        [Fact]
        public void Float32_IsNotPcm()
        {
            using var dir = new TempDir();
            var path = WavFixtures.MonoFloat32(dir.File("float.wav"));

            Assert.True(analyzer.TryAnalyze(path, out var info, out _));
            Assert.False(info!.IsPcm);
            Assert.False(info.IsCompatible);
            Assert.Contains("not 16-bit PCM", info.GetIssuesSummary());
        }

        [Fact]
        public void JunkFile_FailsToAnalyze()
        {
            using var dir = new TempDir();
            var path = WavFixtures.JunkHeader(dir.File("junk.wav"));

            Assert.False(analyzer.TryAnalyze(path, out var info, out var error));
            Assert.Null(info);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void MissingFile_FailsToAnalyze()
        {
            Assert.False(analyzer.TryAnalyze("does-not-exist.wav", out _, out var error));
            Assert.Equal("File does not exist.", error);
        }
    }
}
