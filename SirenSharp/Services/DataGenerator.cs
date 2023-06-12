using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SirenSharp.Models;

namespace SirenSharp.Services
{
    public class DataGenerator : IDataGenerator
    {
        public string GenerateNametable(List<SoundSet> soundSets)
        {
            var nametableText = "";
            foreach (var soundSet in soundSets)
            {
                foreach (var sound in soundSet.Sounds)
                {
                    nametableText += $"{sound.Name.ToLower()}_s\0";
                }
                nametableText += $"{soundSet.Name.ToLower()}_soundset\0";
            }
            return nametableText;
        }

        public string GenerateDatXml(DLC dlc, List<SoundSet> soundSets)
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
                foreach (var soundSet in soundSets)
                {
                    xml.WriteStartElement("Item");
                    xml.WriteString($"DLC_{dlc.Name.ToUpper()}\\{soundSet.Name.ToUpper()}");
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();

                xml.WriteStartElement("Items");

                // SOUND SET ITEM
                foreach (var soundSet in soundSets)
                {
                    xml.WriteStartElement("Item");
                    xml.WriteAttributeString("type", "SoundSet");

                    xml.WriteStartElement("Name");
                    xml.WriteString($"{soundSet.Name.ToLower()}_soundset");
                    xml.WriteEndElement();

                    xml.WriteStartElement("Header");
                    xml.WriteStartElement("Flags");
                    xml.WriteAttributeString("value", "0xAAAAAAAA");
                    xml.WriteEndElement();
                    xml.WriteEndElement();

                    xml.WriteStartElement("SoundSets");

                    foreach (var sound in soundSet.Sounds)
                    {
                        xml.WriteStartElement("Item");
                        xml.WriteStartElement("ScriptName");
                        xml.WriteString(sound.Name.ToLower());
                        xml.WriteEndElement();
                        xml.WriteStartElement("ChildSound");
                        xml.WriteString(sound.Name.ToLower() + "_d");
                        xml.WriteEndElement();
                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement(); // SoundSets

                    xml.WriteEndElement(); // Item

                    foreach (var sound in soundSet.Sounds)
                    {
                        xml.WriteStartElement("Item");
                        xml.WriteAttributeString("type", "SimpleSound");

                        xml.WriteStartElement("Name");
                        xml.WriteString($"{sound.Name.ToLower()}_s");
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
                        xml.WriteString($"dlc_{dlc.Name}/{soundSet.Name.ToLower()}");
                        xml.WriteEndElement();

                        xml.WriteStartElement("FileName");
                        xml.WriteString($"{sound.Name.ToLower()}");
                        xml.WriteEndElement();

                        xml.WriteStartElement("WaveSlotNum");
                        xml.WriteAttributeString("value", "0");
                        xml.WriteEndElement();

                        xml.WriteEndElement(); // Item

                        xml.WriteStartElement("Item");
                        xml.WriteAttributeString("type", "DirectionalSound");

                        xml.WriteStartElement("Name");
                        xml.WriteString($"{sound.Name.ToLower()}_d");
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
                        xml.WriteString($"{sound.Name.ToLower()}_s");
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
                }

                xml.WriteEndElement(); // Items

                xml.WriteEndElement(); // Dat54

                xml.WriteEndDocument();

                xml.Flush();

                return Encoding.UTF8.GetString(stream.ToArray()).Replace("\uFEFF", "");
            }  
        }
    }
}
