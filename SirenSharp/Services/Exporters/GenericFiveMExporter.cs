using System.Reflection;
using System.Text;
using SirenSharp.Models;
using SirenSharp.Services.Backends;
using SirenSharp.Services.Backends.Native;

namespace SirenSharp.Services.Exporters
{
    /// <summary>
    /// The default export target: a ready-to-drop FiveM resource (fxmanifest.lua, dlc_*
    /// AWC files, dat54 data, notes, and an optional in-game tester). Delegates all GTA
    /// audio encoding to <see cref="AudioPackBuilder"/> and owns only the FiveM-specific
    /// directory layout and packaging.
    /// </summary>
    public sealed class GenericFiveMExporter : IResourceExporter
    {
        private readonly AudioPackBuilder audioPackBuilder;
        private readonly CodeWalkerAwcBuildBackend codeWalkerBackend;
        private readonly NativeAwcBuildBackend nativeBackend;

        public GenericFiveMExporter(
            AudioPackBuilder audioPackBuilder,
            CodeWalkerAwcBuildBackend codeWalkerBackend,
            NativeAwcBuildBackend nativeBackend)
        {
            this.audioPackBuilder = audioPackBuilder;
            this.codeWalkerBackend = codeWalkerBackend;
            this.nativeBackend = nativeBackend;
        }

        public string Id => "fivem-generic";

        public string DisplayName => "Generic FiveM Resource";

        public ResourceGenerationResult Export(ResourceGenerationOptions options, IProgress<string>? progress = null)
        {
            var result = new ResourceGenerationResult();
            var dlc = new DLC(options.DlcName) { SoundSets = options.SoundSets };

            var resourceDir = Path.Combine(options.FolderPath, options.ResourceName);
            var dataDir = Path.Combine(resourceDir, "data");
            var dlcDir = Path.Combine(resourceDir, $"dlc_{dlc.Name}");
            var rawDir = Path.Combine(resourceDir, "raw");

            try
            {
                Directory.CreateDirectory(resourceDir);
                Directory.CreateDirectory(dataDir);
                Directory.CreateDirectory(dlcDir);
                Directory.CreateDirectory(rawDir);

                IAwcBuildBackend backend = options.UseNativeAwcBackend ? nativeBackend : codeWalkerBackend;
                if (!audioPackBuilder.Build(dlc, rawDir, dlcDir, dataDir, backend, result, progress))
                {
                    return result;
                }

                if (Directory.Exists(rawDir))
                {
                    Directory.Delete(rawDir, true);
                }

                File.WriteAllText(Path.Combine(resourceDir, "fxmanifest.lua"), BuildManifest(options, dlc));
                File.WriteAllText(Path.Combine(resourceDir, "SIRENSHARP_NOTES.txt"), BuildResourceNotes(options));

                if (options.GenerateInGameTester)
                {
                    progress?.Report("Writing in-game tester...");
                    try
                    {
                        result.TesterPath = EmitInGameTester(options);
                    }
                    catch (Exception ex)
                    {
                        result.Diagnostics.AddWarning(
                            $"In-game tester was not generated: {ex.Message}",
                            DiagnosticCodes.TesterFailed);
                    }
                }

                result.Success = true;
                result.ResourcePath = resourceDir;
                return result;
            }
            catch (Exception ex)
            {
                result.Diagnostics.AddError(ex.Message, DiagnosticCodes.Unexpected);
                return result;
            }
            finally
            {
                if (Directory.Exists(rawDir))
                {
                    try { Directory.Delete(rawDir, true); } catch { /* ignore */ }
                }
            }
        }

        private static string BuildManifest(ResourceGenerationOptions options, DLC dlc)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"fx_version '{options.FxVersion}'");
            sb.AppendLine("game 'gta5'");
            sb.AppendLine();
            sb.AppendLine("author 'BJDubb'");
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            sb.AppendLine($"description 'Generated with SirenSharp v{version?.Major}.{version?.Minor}'");
            sb.AppendLine();
            sb.AppendLine("files {");

            foreach (var soundSet in dlc.SoundSets)
            {
                sb.AppendLine($"\t'dlc_{dlc.Name}/{soundSet.Name}.awc',");
            }

            sb.AppendLine($"\t'data/{options.DlcName}.dat54.rel',");
            sb.AppendLine($"\t'data/{options.DlcName}.dat54.nametable',");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"data_file \"AUDIO_WAVEPACK\" \"dlc_{dlc.Name}\"");
            sb.AppendLine($"data_file \"AUDIO_SOUNDDATA\" \"data/{dlc.Name}.dat\"");

            return sb.ToString();
        }

        private static string BuildResourceNotes(ResourceGenerationOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SirenSharp - generated resource notes");
            sb.AppendLine("=====================================");
            sb.AppendLine();
            sb.AppendLine("If sirens play in OpenIV/CodeWalker but NOT in-game:");
            sb.AppendLine("  • With LVC Fleet: disable Local Override in VCF files AND in-game O-menu.");
            sb.AppendLine("  • Verify VCF String/Ref/Bank match this resource's soundset names.");
            sb.AppendLine("  • Run /refresh and ensure the resource is started on your server.");
            sb.AppendLine();
            sb.AppendLine("Source WAV requirements:");
            sb.AppendLine("  • Mono channel (not stereo)");
            sb.AppendLine("  • 16-bit PCM encoding");
            sb.AppendLine();
            sb.AppendLine("AWC hex names in OpenIV are normal - use your string names in LVC VCF.");
            sb.AppendLine();
            sb.AppendLine($"Resource: {options.ResourceName}");
            sb.AppendLine($"DLC: dlc_{options.DlcName}");
            sb.AppendLine($"fx_version: {options.FxVersion}");
            sb.AppendLine();
            if (options.GenerateInGameTester)
            {
                sb.AppendLine("In-game tester:");
                sb.AppendLine("  The 'sirensharp-audio-test' resource next to this pack plays these");
                sb.AppendLine("  sirens in-game (RageUI menu, /sirentest) with no LVC config.");
                sb.AppendLine();
            }
            sb.AppendLine("Docs: https://docs.sirensharp.dev");
            return sb.ToString();
        }

        // Writes a ready-to-run copy of the bundled FiveM tester next to the pack,
        // with config.lua pre-filled from this project's soundset/siren names. The
        // RageUI + script template is embedded in the assembly (see csproj).
        private static string EmitInGameTester(ResourceGenerationOptions options)
        {
            var testerDir = Path.Combine(options.FolderPath, "sirensharp-audio-test");
            if (Directory.Exists(testerDir))
                Directory.Delete(testerDir, true);
            Directory.CreateDirectory(testerDir);

            var asm = Assembly.GetExecutingAssembly();
            const string prefix = "tester/";
            foreach (var name in asm.GetManifestResourceNames())
            {
                if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;

                var rel = name.Substring(prefix.Length).Replace('\\', '/');
                var outPath = Path.Combine(testerDir, rel.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

                using var src = asm.GetManifestResourceStream(name)!;
                using var dst = File.Create(outPath);
                src.CopyTo(dst);
            }

            File.WriteAllText(Path.Combine(testerDir, "config.lua"), BuildTesterConfig(options));
            return testerDir;
        }

        private static string BuildTesterConfig(ResourceGenerationOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Config = {}");
            sb.AppendLine();
            sb.AppendLine($"-- Generated by SirenSharp for resource '{options.ResourceName}'.");
            sb.AppendLine("-- Names match the generated pack. Adjust Command/Keybind to taste.");
            sb.AppendLine("Config.Command = 'sirentest'");
            sb.AppendLine("Config.Keybind = ''");
            sb.AppendLine();
            sb.AppendLine("Config.Soundsets = {");
            foreach (var soundSet in options.SoundSets)
            {
                var sirens = string.Join(", ", soundSet.Sounds.Select(s => $"'{EscapeLua(s.Name.ToLower())}'"));
                sb.AppendLine("    {");
                sb.AppendLine($"        dlc = '{EscapeLua(options.DlcName)}',");
                sb.AppendLine($"        name = '{EscapeLua(soundSet.Name.ToLower())}',");
                sb.AppendLine($"        sirens = {{ {sirens} }},");
                sb.AppendLine("    },");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string EscapeLua(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");
    }
}
