using SirenSharp.Models;

namespace SirenSharp.Services.Preflight
{
    /// <summary>
    /// Runs every registered <see cref="IPreflightCheck"/> against a project and
    /// aggregates their findings into a single <see cref="DiagnosticReport"/>. This is
    /// the one place project validation lives now - the UI gate, status bar, and any
    /// future preflight panel all read from the same report.
    /// </summary>
    public sealed class PreflightService
    {
        private readonly IReadOnlyList<IPreflightCheck> checks;

        public PreflightService(IEnumerable<IPreflightCheck> checks)
        {
            this.checks = checks.ToList();
        }

        public DiagnosticReport Inspect(Project project)
        {
            var report = new DiagnosticReport();
            foreach (var check in checks)
            {
                report.AddRange(check.Inspect(project));
            }
            return report;
        }

        /// <summary>
        /// Builds a service with the standard checks wired up, for callers (and tests)
        /// that aren't using the DI container.
        /// </summary>
        public static PreflightService CreateDefault() => new(new IPreflightCheck[]
        {
            new ProjectStructureCheck(),
            new SoundSetCheck(),
            new SirenAudioCheck(),
        });
    }
}
