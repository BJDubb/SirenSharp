using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SirenSharp.Models;
using SirenSharp.Services.Backends;
using SirenSharp.Services.Preflight;

namespace SirenSharp.Services
{
    /// <summary>
    /// Builds and writes a <see cref="DiagnosticsSnapshot"/>. Exports as JSON (machine
    /// readable, for bug reports) or as a plain-text summary, chosen by file extension.
    /// </summary>
    public sealed class DiagnosticsExporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly PreflightService preflight;
        private readonly IAwcBuildBackend backend;

        public DiagnosticsExporter(PreflightService preflight, IAwcBuildBackend backend)
        {
            this.preflight = preflight;
            this.backend = backend;
        }

        /// <summary>
        /// Captures the current state of a project (and an optional just-finished build
        /// result) into a snapshot.
        /// </summary>
        public DiagnosticsSnapshot Capture(Project? project, ResourceGenerationResult? generation = null)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            var snapshot = new DiagnosticsSnapshot
            {
                AppVersion = version?.ToString() ?? "unknown",
                OperatingSystem = RuntimeInformation.OSDescription,
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                // Prefer the backend that actually built the pack; fall back to the default.
                AwcBackend = generation?.AwcBackendName
                    ?? (backend.IsExperimental ? $"{backend.Name} (experimental)" : backend.Name),
                ProjectName = project?.ProjectName,
                DlcName = project?.DLCName,
                SoundSetCount = project?.SoundSets.Count ?? 0,
                SoundCount = project?.SoundSets.Sum(s => s.Sounds.Count) ?? 0,
                Preflight = project != null ? preflight.Inspect(project) : new DiagnosticReport(),
                Generation = generation?.Diagnostics,
                AwcVerifications = generation?.AwcVerifications ?? new List<AwcVerificationResult>(),
            };

            return snapshot;
        }

        /// <summary>Writes a snapshot to disk; .json yields JSON, anything else plain text.</summary>
        public void Export(DiagnosticsSnapshot snapshot, string filePath)
        {
            var isJson = Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase);
            File.WriteAllText(filePath, isJson ? ToJson(snapshot) : ToText(snapshot));
        }

        public string ToJson(DiagnosticsSnapshot snapshot) => JsonSerializer.Serialize(snapshot, JsonOptions);

        public string ToText(DiagnosticsSnapshot snapshot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SirenSharp diagnostics report");
            sb.AppendLine("=============================");
            sb.AppendLine($"Generated:   {snapshot.GeneratedAtUtc:u}");
            sb.AppendLine($"App version: {snapshot.AppVersion}");
            sb.AppendLine($"OS:          {snapshot.OperatingSystem}");
            sb.AppendLine($"Runtime:     {snapshot.RuntimeVersion}");
            sb.AppendLine($"AWC backend: {snapshot.AwcBackend}");
            sb.AppendLine();

            sb.AppendLine("Project");
            sb.AppendLine("-------");
            sb.AppendLine($"Name:      {snapshot.ProjectName ?? "(none open)"}");
            sb.AppendLine($"DLC:       {snapshot.DlcName ?? "-"}");
            sb.AppendLine($"Soundsets: {snapshot.SoundSetCount}");
            sb.AppendLine($"Sirens:    {snapshot.SoundCount}");
            sb.AppendLine();

            AppendReport(sb, "Preflight", snapshot.Preflight);

            if (snapshot.Generation != null)
            {
                AppendReport(sb, "Generation", snapshot.Generation);
            }

            if (snapshot.AwcVerifications.Count > 0)
            {
                sb.AppendLine("AWC verification");
                sb.AppendLine("----------------");
                foreach (var v in snapshot.AwcVerifications)
                    sb.AppendLine($"  {v.Summary}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void AppendReport(StringBuilder sb, string title, DiagnosticReport report)
        {
            sb.AppendLine(title);
            sb.AppendLine(new string('-', title.Length));

            if (report.Items.Count == 0)
            {
                sb.AppendLine("  No findings.");
                sb.AppendLine();
                return;
            }

            foreach (var d in report.Items)
            {
                var code = string.IsNullOrEmpty(d.Code) ? string.Empty : $" [{d.Code}]";
                sb.AppendLine($"  {d.Severity.ToString().ToUpper()}{code}: {d}");
            }
            sb.AppendLine();
        }
    }
}
