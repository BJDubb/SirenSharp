namespace SirenSharp.Models
{
    /// <summary>
    /// An ordered collection of <see cref="Diagnostic"/> findings. This is the shared
    /// currency returned by preflight, generation, and verification so every stage
    /// reports problems the same way.
    /// </summary>
    public sealed class DiagnosticReport
    {
        private readonly List<Diagnostic> items = new();

        public IReadOnlyList<Diagnostic> Items => items;

        public bool HasErrors => items.Any(d => d.Severity == DiagnosticSeverity.Error);
        public bool HasWarnings => items.Any(d => d.Severity == DiagnosticSeverity.Warning);

        public IEnumerable<Diagnostic> Errors => items.Where(d => d.Severity == DiagnosticSeverity.Error);
        public IEnumerable<Diagnostic> Warnings => items.Where(d => d.Severity == DiagnosticSeverity.Warning);

        public Diagnostic Add(Diagnostic diagnostic)
        {
            items.Add(diagnostic);
            return diagnostic;
        }

        public void AddError(string message, string? code = null, string? target = null, string? suggestedAction = null)
            => items.Add(new Diagnostic(DiagnosticSeverity.Error, message, code, target, suggestedAction));

        public void AddWarning(string message, string? code = null, string? target = null, string? suggestedAction = null)
            => items.Add(new Diagnostic(DiagnosticSeverity.Warning, message, code, target, suggestedAction));

        public void AddInfo(string message, string? code = null, string? target = null, string? suggestedAction = null)
            => items.Add(new Diagnostic(DiagnosticSeverity.Info, message, code, target, suggestedAction));

        public void AddRange(IEnumerable<Diagnostic> diagnostics) => items.AddRange(diagnostics);
    }
}
