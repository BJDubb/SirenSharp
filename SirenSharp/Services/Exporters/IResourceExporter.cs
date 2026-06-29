using SirenSharp.Models;

namespace SirenSharp.Services.Exporters
{
    /// <summary>
    /// An export target: turns a set of soundsets into an on-disk deliverable for a
    /// particular consumer (a generic FiveM resource today; LVC:Fleet snippets or other
    /// targets later). Exporters share the GTA audio build via <see cref="AudioPackBuilder"/>
    /// and add their own packaging on top.
    /// </summary>
    public interface IResourceExporter
    {
        /// <summary>Stable id used for selection/persistence, e.g. "fivem-generic".</summary>
        string Id { get; }

        /// <summary>Human-readable name shown in the export target selector.</summary>
        string DisplayName { get; }

        ResourceGenerationResult Export(ResourceGenerationOptions options, IProgress<string>? progress = null);
    }
}
