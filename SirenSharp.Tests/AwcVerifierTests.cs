using System.IO;
using SirenSharp.Services;
using Xunit;

namespace SirenSharp.Tests
{
    public class AwcVerifierTests
    {
        private readonly AwcVerifier verifier = new();

        [Fact]
        public void MissingFile_IsNotHealthy()
        {
            using var dir = new TempDir();
            var result = verifier.Verify("lspd", dir.File("missing.awc"));

            Assert.False(result.IsHealthy);
            Assert.Equal("AWC file was not created.", result.ErrorMessage);
        }

        [Fact]
        public void TinyFile_IsRejectedAsSilentOrBroken()
        {
            using var dir = new TempDir();
            var path = dir.File("tiny.awc");
            File.WriteAllBytes(path, new byte[512]);

            var result = verifier.Verify("lspd", path);

            Assert.False(result.IsHealthy);
            Assert.Contains("bytes", result.ErrorMessage);
        }

        [Fact]
        public void JunkAboveSizeGate_IsRejected()
        {
            using var dir = new TempDir();
            var path = dir.File("junk.awc");
            var junk = new byte[4096];
            new Random(99).NextBytes(junk);
            File.WriteAllBytes(path, junk);

            var result = verifier.Verify("lspd", path);

            Assert.False(result.IsHealthy);
            Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        }
    }
}
