using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;

namespace SirenSharp.Models
{
    public class Sound : ObservableObject
    {
        private int samples;
        private int sampleRate;
        private TimeSpan length;
        private long size;
        private string name;

        public string Name
        {
            get => name; set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        private string audioPath { get; set; }
        public string AudioPath
        {
            get => audioPath; set
            {
                UpdateFilePath(value);
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public int Samples
        {
            get => samples; set
            {
                samples = value;
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public int SampleRate
        {
            get => sampleRate;
            set
            {
                sampleRate = value;
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public TimeSpan Length
        {
            get => length;
            set
            {
                length = value;
                OnPropertyChanged();
            }
        }
        public string FileName => new FileInfo(AudioPath).Name;
        [XmlIgnore]
        public long Size
        {
            get => size;
            set
            {
                size = value;
                OnPropertyChanged();
            }
        }

        public Sound()
        {

        }

        public Sound(string filePath)
        {
            AudioPath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath).ToLower().Replace(" ", "_");
        }

        private void UpdateFilePath(string filePath)
        {
            if (File.Exists(filePath))
            {
                using (var wfr = new WaveFileReader(filePath))
                {
                    Length = wfr.TotalTime;
                    SampleRate = wfr.WaveFormat.SampleRate;
                    int len = (int)wfr.Length;
                    int other = wfr.WaveFormat.Channels * wfr.WaveFormat.BitsPerSample / 8;
                    Samples = len / other;
                }

                Size = new FileInfo(filePath).Length;
            }

            audioPath = filePath;
        }
    }
}
