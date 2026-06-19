using System.Reflection;
using System.Text;
using System.Xml;
using CodeWalker.GameFiles;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public class ResourceGenerator
    {
        private readonly AwcGenerator awcGenerator;
        private readonly DataGenerator dataGenerator;
        private readonly WavSanitizer wavSanitizer;
        private readonly AwcVerifier awcVerifier;

        public ResourceGenerator(
            AwcGenerator awcGenerator,
            DataGenerator dataGenerator,
            WavSanitizer wavSanitizer,
            AwcVerifier awcVerifier)
        {
            this.awcGenerator = awcGenerator;
            this.dataGenerator = dataGenerator;
            this.wavSanitizer = wavSanitizer;
            this.awcVerifier = awcVerifier;
        }

        public ResourceGenerationResult GenerateResource(ResourceGenerationOptions options, IProgress<string>? progress = null)
        {
            var result = new ResourceGenerationResult();
            var dlc = new DLC(options.DlcName);
            dlc.SoundSets = options.SoundSets;

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

                foreach (var soundSet in dlc.SoundSets)
                {
                    progress?.Report($"Preparing audio for '{soundSet.Name}'...");
                    var awcDir = Directory.CreateDirectory(Path.Combine(rawDir, soundSet.Name));

                    foreach (var sound in soundSet.Sounds)
                    {
                        var destPath = Path.Combine(awcDir.FullName, sound.FileName);
                        var sanitize = wavSanitizer.Sanitize(sound.AudioPath, destPath);

                        if (!sanitize.Success)
                        {
                            result.Errors.Add($"Failed to prepare WAV for {soundSet.Name}/{sound.Name}: {sanitize.Error}");
                            return result;
                        }

                        if (sanitize.WasConverted)
                        {
                            result.Warnings.Add($"{soundSet.Name}/{sound.Name}: auto-converted ({string.Join(", ", sanitize.Changes)})");
                        }
                    }

                    progress?.Report($"Building AWC '{soundSet.Name}' ({soundSet.Sounds.Count} sirens)...");
                    var awcXml = awcGenerator.GenerateAwcXml(soundSet);
                    var awcDoc = new XmlDocument();
                    awcDoc.LoadXml(awcXml);

                    byte[] data;
                    try
                    {
                        data = XmlMeta.GetData(awcDoc, MetaFormat.Awc, awcDir.FullName) ?? Array.Empty<byte>();
                    }
                    catch (WavImportException wex)
                    {
                        result.Errors.Add(DescribeWavImportError(soundSet.Name, wex));
                        return result;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error generating AWC '{soundSet.Name}': {ex.Message}");
                        return result;
                    }

                    if (data.Length == 0)
                    {
                        result.Errors.Add($"AWC '{soundSet.Name}' produced no data.");
                        return result;
                    }

                    var awcOutputPath = Path.Combine(dlcDir, soundSet.Name + ".awc");
                    File.WriteAllBytes(awcOutputPath, data);

                    var verification = awcVerifier.Verify(soundSet.Name, awcOutputPath);
                    result.AwcVerifications.Add(verification);

                    if (!verification.IsHealthy)
                    {
                        result.Errors.Add(verification.Summary);
                        return result;
                    }
                }

                progress?.Report("Writing dat54 metadata and manifest...");
                var nametableText = dataGenerator.GenerateNametable(options.SoundSets);
                var datXml = dataGenerator.GenerateDatXml(dlc, options.SoundSets);

                var doc = new XmlDocument();
                doc.LoadXml(datXml);
                var datData = XmlMeta.GetData(doc, MetaFormat.AudioRel, string.Empty) ?? Array.Empty<byte>();

                var nametableFile = $"{dlc.Name}.dat54.nametable";
                var datFile = $"{dlc.Name}.dat54.rel";

                File.WriteAllText(Path.Combine(dataDir, nametableFile), nametableText);
                File.WriteAllBytes(Path.Combine(dataDir, datFile), datData);

                if (Directory.Exists(rawDir))
                {
                    Directory.Delete(rawDir, true);
                }

                var manifest = BuildManifest(options, dlc);
                File.WriteAllText(Path.Combine(resourceDir, "fxmanifest.lua"), manifest);
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
                        result.Warnings.Add($"In-game tester was not generated: {ex.Message}");
                    }
                }

                result.Success = true;
                result.ResourcePath = resourceDir;
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
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

        private static string DescribeWavImportError(string soundSetName, WavImportException wex)
        {
            var fileLabel = string.IsNullOrEmpty(wex.FilePath) ? soundSetName : $"{soundSetName}/{wex.FilePath}";

            var guidance = wex.Reason switch
            {
                WavImportError.UnsupportedEncoding =>
                    "the WAV is not 16-bit PCM. Use 'Fix Audio' or re-export from Audacity as 16-bit PCM.",
                WavImportError.UnsupportedChannelCount =>
                    $"the WAV is not mono ({wex.Channels?.ToString() ?? "multi"}-channel). Use 'Fix Audio' to downmix to mono.",
                WavImportError.NoAudioData =>
                    "the WAV contains no audio samples. Re-export a valid clip.",
                WavImportError.FileReadError =>
                    "the WAV file could not be read. Make sure it still exists and isn't open in another program.",
                _ => wex.Message,
            };

            return $"Could not build AWC for {fileLabel}: {guidance}";
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
