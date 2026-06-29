using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace SirenSharp.Services
{
    /// <summary>
    /// Wraps Velopack's <see cref="UpdateManager"/> so the rest of the app can check for,
    /// download, and apply updates without depending on Velopack directly. When the app is
    /// running as a plain portable exe (not a Velopack install) this degrades gracefully:
    /// <see cref="IsInstalled"/> is false and checks are skipped.
    /// </summary>
    public sealed class UpdateService
    {
        private const string RepoUrl = "https://github.com/BJDubb/SirenSharp";

        private readonly UpdateManager manager;
        private UpdateInfo? pending;

        public UpdateService()
        {
            manager = new UpdateManager(new GithubSource(RepoUrl, null, false));
        }

        /// <summary>True only when launched from a Velopack-managed install.</summary>
        public bool IsInstalled => manager.IsInstalled;

        /// <summary>Release channel this build updates from.</summary>
        public string Channel { get; } = "stable";

        /// <summary>
        /// Version string for display. Uses the Velopack install version when available,
        /// otherwise the assembly version (portable builds).
        /// </summary>
        public string CurrentVersion
        {
            get
            {
                var v = manager.CurrentVersion;
                if (v != null) return v.ToString();
                var a = Assembly.GetExecutingAssembly().GetName().Version;
                return a == null ? "unknown" : $"{a.Major}.{a.Minor}.{a.Build}";
            }
        }

        /// <summary>
        /// Checks the release feed. Returns the new version string if an update is waiting,
        /// or null if up to date / not a managed install / offline.
        /// </summary>
        public async Task<string?> CheckForUpdatesAsync()
        {
            if (!manager.IsInstalled) return null;

            pending = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            return pending?.TargetFullRelease.Version.ToString();
        }

        /// <summary>Downloads the pending update and restarts into it. No-op if nothing pending.</summary>
        public async Task DownloadAndApplyAsync()
        {
            if (pending == null) return;

            await manager.DownloadUpdatesAsync(pending).ConfigureAwait(false);
            manager.ApplyUpdatesAndRestart(pending);
        }
    }
}
