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
    public class AwcGenerator : IAwcGenerator
    {
        public string GenerateAwcXml(SoundSet soundSet)
        {
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

                foreach (var sound in soundSet.Sounds)
                {
                    xml.WriteStartElement("Item");

                    xml.WriteStartElement("Name");
                    xml.WriteString(sound.Name);
                    xml.WriteEndElement();

                    xml.WriteStartElement("FileName");
                    xml.WriteString(sound.FileName);
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
                    xml.WriteAttributeString("value", sound.Samples.ToString());
                    xml.WriteEndElement();

                    xml.WriteStartElement("SampleRate");
                    xml.WriteAttributeString("value", sound.SampleRate.ToString());
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

                return Encoding.UTF8.GetString(stream.ToArray()).Replace("\uFEFF", "");
            }
        }
    }
}
