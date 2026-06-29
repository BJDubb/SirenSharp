using SirenSharp.Models;

namespace SirenSharp.Services.Preflight
{
    /// <summary>
    /// A single pre-generation validation rule. Checks inspect the project and yield
    /// structured <see cref="Diagnostic"/> findings instead of throwing or returning
    /// bare strings, so the UI can group them, show suggested actions, and let the user
    /// fix issues before a build is attempted.
    /// </summary>
    public interface IPreflightCheck
    {
        IEnumerable<Diagnostic> Inspect(Project project);
    }
}
