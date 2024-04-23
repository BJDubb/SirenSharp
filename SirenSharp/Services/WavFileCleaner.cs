using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSCore;
using CSCore.Codecs.WAV;

namespace SirenSharp.Services
{
    public class WavFileCleaner
    {
        public void Clean(string inputPath, string outputPath)
        {
            var waveFormat = new WaveFormat();

            using (var fileStream = File.OpenRead(inputPath))
            using (var reader = new BinaryReader(fileStream))
            using (var waveWriter = new WaveWriter(outputPath, waveFormat))
            {
                reader.BaseStream.Seek(12, SeekOrigin.Begin);

                while (fileStream.Position < fileStream.Length)
                {
                    string chunkId = new string(reader.ReadChars(4));
                    int chunkSize = reader.ReadInt32();

                    switch (chunkId)
                    {
                        case "fmt ":
                            byte[] fmtChunk = reader.ReadBytes(chunkSize);
                            waveWriter.Write(fmtChunk, 0, fmtChunk.Length);
                            break;

                        case "data":
                            byte[] dataChunk = reader.ReadBytes(chunkSize);
                            waveWriter.Write(dataChunk, 0, dataChunk.Length);
                            break;

                        default:
                            reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                            break;
                    }
                }
            }
        }
    }
}
