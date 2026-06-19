using System.Diagnostics;

namespace SirenSharp.Services
{
    public class ExternalToolLauncher
    {
        public string? CodeWalkerPath { get; set; }

        public bool TryDetectTools()
        {
            if (!string.IsNullOrWhiteSpace(CodeWalkerPath) && File.Exists(CodeWalkerPath))
                return true;

            foreach (var candidate in GetCodeWalkerCandidates())
            {
                if (File.Exists(candidate))
                {
                    CodeWalkerPath = candidate;
                    return true;
                }
            }

            return false;
        }

        // CodeWalker has no command-line argument for opening a specific folder or
        // file - Program.Main only understands bare mode flags (menu/explorer/...).
        // The best we can do programmatically is launch its RPF Explorer mode; the
        // user then opens the resource folder via File -> Open Folder. We copy the
        // path to the clipboard at the call site to make that a single paste.
        public bool TryOpenRpfExplorer()
        {
            TryDetectTools();
            if (string.IsNullOrWhiteSpace(CodeWalkerPath) || !File.Exists(CodeWalkerPath))
                return false;

            var dir = Path.GetDirectoryName(CodeWalkerPath)!;

            // Prefer the dedicated RPF Explorer launcher shipped alongside CodeWalker.
            var explorerExe = Path.Combine(dir, "CodeWalker RPF Explorer.exe");
            if (File.Exists(explorerExe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = explorerExe,
                    WorkingDirectory = dir,
                    UseShellExecute = true
                });
                return true;
            }

            // Otherwise launch the main exe directly into explorer mode.
            Process.Start(new ProcessStartInfo
            {
                FileName = CodeWalkerPath,
                Arguments = "explorer",
                WorkingDirectory = dir,
                UseShellExecute = true
            });
            return true;
        }

        private static IEnumerable<string> GetCodeWalkerCandidates()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            yield return Path.Combine(programFiles, "CodeWalker", "CodeWalker.exe");
            yield return Path.Combine(programFilesX86, "CodeWalker", "CodeWalker.exe");
            yield return Path.Combine(desktop, "CodeWalker", "CodeWalker.exe");
            yield return Path.Combine(downloads, "CodeWalker", "CodeWalker.exe");
        }
    }
}
