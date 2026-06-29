using System.Linq;
using SirenSharp.Models;
using SirenSharp.Services.Preflight;
using Xunit;

namespace SirenSharp.Tests
{
    public class PreflightServiceTests
    {
        private static readonly PreflightService Preflight = PreflightService.CreateDefault();

        [Fact]
        public void EmptyProject_IsBlockingError()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));

            var report = Preflight.Inspect(project);

            Assert.True(report.HasErrors);
            Assert.Contains(report.Items, d => d.Code == DiagnosticCodes.ProjectEmpty);
        }

        [Fact]
        public void DuplicateSoundsetNames_AreBlockingError()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));
            project.SoundSets.Add(new SoundSet("lspd"));
            project.SoundSets.Add(new SoundSet("lspd"));

            var report = Preflight.Inspect(project);

            Assert.True(report.HasErrors);
            Assert.Contains(report.Errors, d => d.Code == DiagnosticCodes.AwcDuplicateName);
        }

        [Fact]
        public void MissingWavFile_IsBlockingError_WithSuggestedAction()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));
            var set = new SoundSet("lspd");
            set.AddSound(new Sound { Name = "wail", AudioPath = @"C:\does\not\exist.wav" });
            project.SoundSets.Add(set);

            var report = Preflight.Inspect(project);

            var missing = Assert.Single(report.Errors, d => d.Code == DiagnosticCodes.SirenMissingFile);
            Assert.Equal("lspd/wail", missing.Target);
            Assert.False(string.IsNullOrEmpty(missing.SuggestedAction));
        }

        [Fact]
        public void EmptySoundset_IsWarningNotError()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));
            project.SoundSets.Add(new SoundSet("lspd"));

            var report = Preflight.Inspect(project);

            Assert.Contains(report.Warnings, d => d.Code == DiagnosticCodes.AwcNoSirens);
            // An empty soundset alone must not block generation.
            Assert.DoesNotContain(report.Errors, d => d.Code == DiagnosticCodes.AwcNoSirens);
        }
    }
}
