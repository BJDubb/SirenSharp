using SirenSharp.Models;

namespace SirenSharp.Services.Backends
{
    /// <summary>
    /// Produces and verifies AWC (Audio Wave Container) bytes for a soundset. The
    /// build pipeline depends only on this seam, so the underlying engine - currently
    /// CodeWalker's XML-meta path, potentially a native RAGE-style encoder later - can
    /// be swapped without touching exporters or the orchestrator.
    /// </summary>
    public interface IAwcBuildBackend
    {
        /// <summary>Display name shown in diagnostics and (eventually) the backend selector.</summary>
        string Name { get; }

        /// <summary>True for backends not yet trusted for production output.</summary>
        bool IsExperimental { get; }

        /// <summary>
        /// Builds AWC bytes for a soundset whose source WAVs have already been
        /// sanitized into <paramref name="preparedWavDirectory"/>.
        /// </summary>
        AwcBuildResult BuildAwc(SoundSet soundSet, string preparedWavDirectory);

        /// <summary>Inspects already-written AWC bytes for silent/empty/corrupt output.</summary>
        AwcVerificationResult Verify(string soundSetName, string awcFilePath);
    }

    /// <summary>Outcome of an AWC build: the bytes, or a diagnostic describing why not.</summary>
    public sealed class AwcBuildResult
    {
        public bool Success { get; private init; }
        public byte[] Data { get; private init; } = Array.Empty<byte>();
        public Diagnostic? Error { get; private init; }

        public static AwcBuildResult Ok(byte[] data) => new() { Success = true, Data = data };
        public static AwcBuildResult Fail(Diagnostic error) => new() { Success = false, Error = error };
    }
}
