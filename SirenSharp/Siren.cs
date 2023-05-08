using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace SirenSharp
{
    public class Siren
    {
        public string SirenName { get; set; }
        private string audioPath { get; set; }
        public string AudioPath { get => audioPath; set => UpdateFilePath(value); }
        public int Samples { get; private set; }
        public int SampleRate { get; private set; }
        public TimeSpan Length { get; private set; }
        public string FileName => new FileInfo(AudioPath).Name;

        public Siren(string sirenName, string audioPath)
        {
            SirenName = sirenName;
            AudioPath = audioPath;
        }

        private void UpdateFilePath(string filePath)
        {
            audioPath = filePath;

            using (var wfr = new WaveFileReader(AudioPath))
            {
                Length = wfr.TotalTime;
                SampleRate = wfr.WaveFormat.SampleRate;
                int len = (int)wfr.Length;
                int other = (wfr.WaveFormat.Channels * wfr.WaveFormat.BitsPerSample / 8);
                Samples = len / other;
            }
        }

    }
}
