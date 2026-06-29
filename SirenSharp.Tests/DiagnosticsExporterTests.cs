using System.IO;
using SirenSharp.Models;
using SirenSharp.Services;
using SirenSharp.Services.Backends;
using SirenSharp.Services.Preflight;
using Xunit;

namespace SirenSharp.Tests
{
    public class DiagnosticsExporterTests
    {
        private static DiagnosticsExporter BuildExporter() =>
            new DiagnosticsExporter(
                PreflightService.CreateDefault(),
                new CodeWalkerAwcBuildBackend(new AwcVerifier()));

        [Fact]
        public void Capture_IncludesPreflightFindings_AndEnvironment()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj")); // empty -> preflight error

            var snapshot = BuildExporter().Capture(project);

            Assert.Equal("demo", snapshot.ProjectName);
            Assert.True(snapshot.Preflight.HasErrors);
            Assert.False(string.IsNullOrEmpty(snapshot.AppVersion));
            Assert.False(string.IsNullOrEmpty(snapshot.RuntimeVersion));
            Assert.Equal("CodeWalker", snapshot.AwcBackend);
            Assert.Null(snapshot.Generation);
        }

        [Fact]
        public void Export_TextReport_ContainsFindings()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));
            var exporter = BuildExporter();

            var outPath = dir.File("report.txt");
            exporter.Export(exporter.Capture(project), outPath);

            var text = File.ReadAllText(outPath);
            Assert.Contains("SirenSharp diagnostics report", text);
            Assert.Contains("Preflight", text);
            Assert.Contains(DiagnosticCodes.ProjectEmpty, text);
        }

        [Fact]
        public void Export_JsonReport_IsValidJsonWithEnumNames()
        {
            using var dir = new TempDir();
            var project = new Project("demo", dir.File("demo.ssproj"));
            var exporter = BuildExporter();

            var outPath = dir.File("report.json");
            exporter.Export(exporter.Capture(project), outPath);

            var json = File.ReadAllText(outPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json); // throws if invalid
            Assert.Contains("Error", json); // severity serialized as name, not number
        }
    }
}
