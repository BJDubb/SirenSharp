using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CodeWalker.GameFiles;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SirenSharp
{
    public class SirenBuilder
    {
        public static void GenerateSirenAWC(string awcName, string wavFolder, Siren[] sirens, string outputPath)
        {
            var awcXml = "";

            var stream = new MemoryStream();
            using (var xml = new XmlTextWriter(stream, Encoding.UTF8))
            {
                xml.Formatting = Formatting.Indented;
                xml.Indentation = 4;

                xml.WriteStartDocument();

                xml.WriteStartElement("AudioWaveContainer");

                xml.WriteStartElement("Version");
                xml.WriteAttributeString("value", "1");
                xml.WriteEndElement();

                xml.WriteStartElement("ChunkIndices");
                xml.WriteAttributeString("value", "True");
                xml.WriteEndElement();

                xml.WriteStartElement("Streams");

                foreach (var siren in sirens)
                {
                    xml.WriteStartElement("Item");

                    xml.WriteStartElement("Name");
                    xml.WriteString(siren.SirenName);
                    xml.WriteEndElement();

                    xml.WriteStartElement("FileName");
                    xml.WriteString(siren.FileName);
                    xml.WriteEndElement();

                    xml.WriteStartElement("Chunks");

                    xml.WriteStartElement("Item");
                    xml.WriteStartElement("Type");
                    xml.WriteString("peak");
                    xml.WriteEndElement();
                    xml.WriteEndElement();

                    xml.WriteStartElement("Item");
                    xml.WriteStartElement("Type");
                    xml.WriteString("data");
                    xml.WriteEndElement();
                    xml.WriteEndElement();

                    xml.WriteStartElement("Item");

                    xml.WriteStartElement("Type");
                    xml.WriteString("format");
                    xml.WriteEndElement();

                    xml.WriteStartElement("Codec");
                    xml.WriteString("PCM");
                    xml.WriteEndElement();

                    xml.WriteStartElement("Samples");
                    xml.WriteAttributeString("value", siren.Samples.ToString());
                    xml.WriteEndElement();

                    xml.WriteStartElement("SampleRate");
                    xml.WriteAttributeString("value", siren.SampleRate.ToString());
                    xml.WriteEndElement();

                    xml.WriteStartElement("Headroom");
                    xml.WriteAttributeString("value", "-200");
                    xml.WriteEndElement();

                    xml.WriteStartElement("PlayBegin");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteStartElement("PlayEnd");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteStartElement("LoopBegin");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteStartElement("LoopEnd");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteStartElement("LoopPoint");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteStartElement("Peak");
                    xml.WriteAttributeString("unk", "0");
                    xml.WriteEndElement();

                    xml.WriteEndElement();


                    xml.WriteEndElement(); //Chunks

                    xml.WriteEndElement(); // Item
                }

                xml.WriteEndElement(); // Streams

                xml.WriteEndElement(); // AudioWaveContainer

                xml.WriteEndDocument();

                xml.Flush();

                StreamReader sr = new StreamReader(stream, Encoding.UTF8, true);
                stream.Seek(0, SeekOrigin.Begin);
                awcXml = sr.ReadToEnd();
            }

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sirenSharpFolder = Path.Combine(localAppDataPath, "SirenSharp");
            string awcXmlPath = Path.Combine(sirenSharpFolder, "awc.xml");
            File.WriteAllText(awcXmlPath, awcXml);

            var trimlength = 4;
            var mformat = XmlMeta.GetXMLFormat(awcName + ".awc.xml", out trimlength);

            var doc = new XmlDocument();
            doc.LoadXml(awcXml);

            byte[] data = XmlMeta.GetData(doc, mformat, wavFolder); // awc file data

            File.WriteAllBytes(outputPath, data);
        }

        public static void GenerateFiveMResource(string folderPath, string resourceName, string awcName, string dlcName, Siren[] sirens)
        {
            var resourceDir = Directory.CreateDirectory(Path.Combine(folderPath, resourceName));

            var dataDir = Directory.CreateDirectory(Path.Combine(resourceDir.FullName, "data"));

            var dlcDir = Directory.CreateDirectory(Path.Combine(resourceDir.FullName, dlcName));

            GenerateSirenAWC(awcName, Path.GetDirectoryName(sirens[0].AudioPath), sirens, Path.Combine(dlcDir.FullName, awcName + ".awc"));

            var datXml = GenerateDatFile(awcName, dlcName, sirens);

            Debug.WriteLine("------ DAT XML -------");
            Debug.WriteLine(datXml);
            Debug.WriteLine("------ DAT XML -------");

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sirenSharpFolder = Path.Combine(localAppDataPath, "SirenSharp");
            string datXmlPath = Path.Combine(sirenSharpFolder, "dat.rel.xml");

            File.WriteAllText(datXmlPath, datXml);

            var nametableText = GenerateNametable(awcName, sirens);

            Debug.WriteLine("------ Nametable -------");
            Debug.WriteLine(nametableText);
            Debug.WriteLine("------ Nametable -------");

            string datRelPath = Path.Combine(sirenSharpFolder, "rel.nametable");

            File.WriteAllText(datRelPath, datXml);

            var trimlength = 4;
            var mformat = XmlMeta.GetXMLFormat("custom_sounds.rel.xml", out trimlength);

            var doc = new XmlDocument();
            doc.LoadXml(datXml);

            byte[] data = XmlMeta.GetData(doc, mformat, ""); // rel file data

            File.WriteAllBytes(Path.Combine(dataDir.FullName, "custom_sounds.dat54.rel"), data); // write rel to disk

            File.WriteAllText(Path.Combine(dataDir.FullName, "custom_sounds.dat54.nametable"), nametableText); // write nametable to disk

            var manifest = $"fx_version 'cerulean'\r\ngame 'gta5'\r\n\r\nauthor 'BJDubb'\r\ndescription 'SirenSharp'\r\nversion '0.0.1'\r\n\r\nfiles {{ \r\n\t\"{dlcName}/{awcName.ToLower()}.awc\",\r\n\t\"data/custom_sounds.dat54.nametable\",\r\n\t\"data/custom_sounds.dat54.rel\"\r\n}}\r\n\r\ndata_file \"AUDIO_WAVEPACK\" \"{dlcName}\"\r\ndata_file \"AUDIO_SOUNDDATA\" \"data/custom_sounds.dat\"";

            File.WriteAllText(Path.Combine(resourceDir.FullName, "fxmanifest.lua"), manifest);
        }

        private static string GenerateNametable(string awcName, Siren[] sirens)
        {
            var nametableText = "";
            foreach (var siren in sirens)
            {
                nametableText += $"{siren.SirenName.ToLower()}_s\0";
            }
            nametableText += $"{awcName.ToLower()}_soundset\0";
            return nametableText;
        }

        private static string GenerateDatFile(string awcName, string dlcName, Siren[] sirens)
        {
            var stream = new MemoryStream();
            using (var xml = new XmlTextWriter(stream, Encoding.UTF8))
            {
                xml.Formatting = Formatting.Indented;
                xml.Indentation = 4;

                xml.WriteStartDocument();

                xml.WriteStartElement("Dat54");

                xml.WriteStartElement("Version");
                xml.WriteAttributeString("value", "7126027");
                xml.WriteEndElement();

                xml.WriteStartElement("ContainerPaths");
                xml.WriteStartElement("Item");
                xml.WriteString($"{dlcName.ToUpper()}\\{awcName.ToUpper()}");
                xml.WriteEndElement();
                xml.WriteEndElement();

                xml.WriteStartElement("Items");

                // SOUND SET ITEM
                xml.WriteStartElement("Item");
                xml.WriteAttributeString("type", "SoundSet");

                xml.WriteStartElement("Name");
                xml.WriteString($"{awcName.ToLower()}_soundset");
                xml.WriteEndElement();

                xml.WriteStartElement("Header");
                xml.WriteStartElement("Flags");
                xml.WriteAttributeString("value", "0xAAAAAAAA");
                xml.WriteEndElement();
                xml.WriteEndElement();

                xml.WriteStartElement("SoundSets");

                foreach (var siren in sirens)
                {
                    xml.WriteStartElement("Item");
                    xml.WriteStartElement("ScriptName");
                    xml.WriteString(siren.SirenName.ToLower());
                    xml.WriteEndElement();
                    xml.WriteStartElement("ChildSound");
                    xml.WriteString(siren.SirenName.ToLower() + "_d");
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                }

                xml.WriteEndElement(); // SoundSets

                xml.WriteEndElement(); // Item

                foreach (var siren in sirens)
                {
                    xml.WriteStartElement("Item");
                    xml.WriteAttributeString("type", "SimpleSound");

                    xml.WriteStartElement("Name");
                    xml.WriteString($"{siren.SirenName.ToLower()}_s");
                    xml.WriteEndElement();

                    xml.WriteStartElement("Header");

                    xml.WriteStartElement("Flags");
                    xml.WriteAttributeString("value", "0x0820E010");
                    xml.WriteEndElement();
                    xml.WriteStartElement("Pitch");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();
                    xml.WriteStartElement("ReleaseTime");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();
                    xml.WriteStartElement("DopplerFactor");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();
                    xml.WriteStartElement("Category");
                    xml.WriteString("vehicles_sirens");
                    xml.WriteEndElement();
                    xml.WriteStartElement("VolumeCurveScale");
                    xml.WriteAttributeString("value", "300");
                    xml.WriteEndElement();
                    xml.WriteStartElement("Unk22");
                    xml.WriteAttributeString("value", "50");
                    xml.WriteEndElement();

                    xml.WriteEndElement(); // Header

                    xml.WriteStartElement("ContainerName");
                    xml.WriteString($"{dlcName}/{awcName.ToLower()}");
                    xml.WriteEndElement();

                    xml.WriteStartElement("FileName");
                    xml.WriteString($"{siren.SirenName.ToLower()}");
                    xml.WriteEndElement();

                    xml.WriteStartElement("WaveSlotNum");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteEndElement(); // Item

                    xml.WriteStartElement("Item");
                    xml.WriteAttributeString("type", "DirectionalSound");

                    xml.WriteStartElement("Name");
                    xml.WriteString($"{siren.SirenName.ToLower()}_d");
                    xml.WriteEndElement();

                    xml.WriteStartElement("Header");

                    xml.WriteStartElement("Flags");
                    xml.WriteAttributeString("value", "0x00008004");
                    xml.WriteEndElement();
                    xml.WriteStartElement("Volume");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();
                    xml.WriteStartElement("Category");
                    xml.WriteString("vehicles_sirens");
                    xml.WriteEndElement();

                    xml.WriteEndElement(); // Header

                    xml.WriteStartElement("ChildSound");
                    xml.WriteString($"{siren.SirenName.ToLower()}_s");
                    xml.WriteEndElement();

                    xml.WriteStartElement("InnerAngle");
                    xml.WriteAttributeString("value", "20");
                    xml.WriteEndElement();

                    xml.WriteStartElement("OuterAngle");
                    xml.WriteAttributeString("value", "65");
                    xml.WriteEndElement();

                    xml.WriteStartElement("RearAttenuation");
                    xml.WriteAttributeString("value", "-3");
                    xml.WriteEndElement();

                    xml.WriteStartElement("YawAngle");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteStartElement("PitchAngle");
                    xml.WriteAttributeString("value", "0");
                    xml.WriteEndElement();

                    xml.WriteEndElement(); // Item
                }

                xml.WriteEndElement(); // Streams

                xml.WriteEndElement(); // AudioWaveContainer

                xml.WriteEndDocument();

                xml.Flush();

                StreamReader sr = new StreamReader(stream, Encoding.UTF8, true);
                stream.Seek(0, SeekOrigin.Begin);
                return sr.ReadToEnd();
            }
        }
    }
}

