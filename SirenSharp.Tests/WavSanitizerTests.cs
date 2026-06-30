using NAudio.Wave;
using SirenSharp.Services;
using Xunit;

namespace SirenSharp.Tests
{
    public class WavSanitizerTests
    {
        private readonly WavSanitizer sanitizer = new(new WavFormatAnalyzer());
        private readonly WavFormatAnalyzer analyzer = new();

        [Fact]
        public void Stereo_DownmixesToCompatibleMono()
        {
            using var dir = new TempDir();
            var input = WavFixtures.StereoPcm16(dir.File("stereo.wav"));
            var output = dir.File("out.wav");

            var result = sanitizer.Sanitize(input, output);

            Assert.True(result.Success, result.Error);
            Assert.True(result.WasConverted);
            Assert.Contains(result.Changes, c => c.Contains("downmix"));

            Assert.True(analyzer.TryAnalyze(output, out var info, out _));
            Assert.True(info!.IsCompatible);
        }

        [Fact]
        public void Float32_ConvertsTo16BitPcm()
        {
            using var dir = new TempDir();
            var input = WavFixtures.MonoFloat32(dir.File("float.wav"));
            var output = dir.File("out.wav");

            var result = sanitizer.Sanitize(input, output);

            Assert.True(result.Success, result.Error);
            Assert.True(result.WasConverted);

            Assert.True(analyzer.TryAnalyze(output, out var info, out _));
            Assert.True(info!.Is16BitPcm);
            Assert.True(info.IsCompatible);
        }

        [Fact]
        public void AlreadyCompatible_RewritesWithoutConversionFlag()
        {
            using var dir = new TempDir();
            var input = WavFixtures.MonoPcm16(dir.File("mono.wav"));
            var output = dir.File("out.wav");

            var result = sanitizer.Sanitize(input, output);

            Assert.True(result.Success, result.Error);
            Assert.False(result.WasConverted);

            Assert.True(analyzer.TryAnalyze(output, out var info, out _));
            Assert.True(info!.IsCompatible);
        }

        [Fact]
        public void Trim_CutsToInAndOutPoints()
        {
            using var dir = new TempDir();
            var input = WavFixtures.MonoPcm16(dir.File("two.wav"), seconds: 2.0);
            var output = dir.File("out.wav");

            var result = sanitizer.Sanitize(input, output, trimStartSeconds: 0.5, trimEndSeconds: 1.5);

            Assert.True(result.Success, result.Error);
            Assert.True(result.WasConverted);
            Assert.Contains(result.Changes, c => c.Contains("trimmed"));

            using var reader = new WaveFileReader(output);
            Assert.InRange(reader.TotalTime.TotalSeconds, 0.95, 1.05);
        }

        [Fact]
        public void Trim_StartOnly_KeepsRemainderToEnd()
        {
            using var dir = new TempDir();
            var input = WavFixtures.MonoPcm16(dir.File("two.wav"), seconds: 2.0);
            var output = dir.File("out.wav");

            var result = sanitizer.Sanitize(input, output, trimStartSeconds: 0.5);

            Assert.True(result.Success, result.Error);
            using var reader = new WaveFileReader(output);
            Assert.InRange(reader.TotalTime.TotalSeconds, 1.45, 1.55);
        }

        [Fact]
        public void JunkFile_FailsGracefully()
        {
            using var dir = new TempDir();
            var input = WavFixtures.JunkHeader(dir.File("junk.wav"));
            var output = dir.File("out.wav");

            var result = sanitizer.Sanitize(input, output);

            Assert.False(result.Success);
            Assert.False(string.IsNullOrEmpty(result.Error));
        }
    }
}
