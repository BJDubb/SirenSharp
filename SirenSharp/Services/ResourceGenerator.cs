using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CodeWalker.GameFiles;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public class ResourceGenerator : IResourceGenerator
    {
        private readonly IAwcGenerator awcGenerator;
        private readonly IDataGenerator dataGenerator;

        public ResourceGenerator(IAwcGenerator awcGenerator, IDataGenerator dataGenerator)
        {
            this.awcGenerator = awcGenerator;
            this.dataGenerator = dataGenerator;
        }

        public void GenerateResource(string resourceName, string dlcName, string folderPath, List<SoundSet> soundSets)
        {
            var dlc = new DLC(dlcName);
            dlc.SoundSets = soundSets;

            var resourceDir = Directory.CreateDirectory(Path.Combine(folderPath, resourceName));

            var dataDir = Directory.CreateDirectory(Path.Combine(resourceDir.FullName, "data"));

            var dlcDir = Directory.CreateDirectory(Path.Combine(resourceDir.FullName, "dlc_" + dlc.Name));

            var rawDir = Directory.CreateDirectory(Path.Combine(resourceDir.FullName, "raw"));

            foreach (var soundSet in dlc.SoundSets) // awc generation
            {
                var awcDir = Directory.CreateDirectory(Path.Combine(rawDir.FullName, soundSet.Name));

                foreach (var sound in soundSet.Sounds) // copy all files into raw dir to build awc
                {
                    File.Copy(sound.AudioPath, $"{awcDir.FullName}/{sound.FileName}", true);
                }

                var awcXml = awcGenerator.GenerateAwcXml(soundSet);

                var awcDoc = new XmlDocument();
                awcDoc.LoadXml(awcXml);

                byte[] data = new byte[] { };

                try
                {
                    data = XmlMeta.GetData(awcDoc, MetaFormat.Awc, awcDir.FullName); // awc file data
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating soundset.\n\n{ex.Message}\nSoundset: {soundSet.Name}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var awcOutputPath = Path.Combine(dlcDir.FullName, soundSet.Name + ".awc");
                File.WriteAllBytes(awcOutputPath, data);
            }

            var nametableText = dataGenerator.GenerateNametable(soundSets);
            var datXml = dataGenerator.GenerateDatXml(dlc, soundSets);

            var doc = new XmlDocument();
            doc.LoadXml(datXml);

            var datData = XmlMeta.GetData(doc, MetaFormat.AudioRel, string.Empty);

            var nametableFile = $"{dlc.Name}.dat54.nametable";
            var datFile = $"{dlc.Name}.dat54.rel";

            File.WriteAllText(Path.Combine(dataDir.FullName, nametableFile), nametableText);
            File.WriteAllBytes(Path.Combine(dataDir.FullName, datFile), datData);

            Directory.Delete(rawDir.FullName, true);

            var sb = new StringBuilder();
            sb.AppendLine("fx_version 'cerulean'");
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

            sb.AppendLine($"\t'data/{dlcName}.dat54.rel',");
            sb.AppendLine($"\t'data/{dlcName}.dat54.nametable',");

            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"data_file \"AUDIO_WAVEPACK\" \"dlc_{dlc.Name}\"");

            foreach (var soundSet in dlc.SoundSets)
            {
                sb.AppendLine($"data_file \"AUDIO_SOUNDDATA\" \"data/{dlc.Name}.dat\"");
            }

            var manifest = sb.ToString();

            File.WriteAllText(Path.Combine(resourceDir.FullName, "fxmanifest.lua"), manifest);
        }
    }
}
