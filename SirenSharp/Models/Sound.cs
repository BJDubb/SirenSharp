using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;
using SirenSharp.Services;
using System.Xml.Serialization;

namespace SirenSharp.Models
{
    public enum SoundFormatState
    {
        Missing,
        Ok,
        NeedsConversion,
    }

    public partial class Sound : ObservableObject
    {
        private int samples;
        private int sampleRate;
        private TimeSpan length;
        private long size;
        private string name = string.Empty;
        private string audioPath = string.Empty;
        private string formatStatus = string.Empty;
        private bool needsConversion;
        private SoundFormatState formatState = SoundFormatState.Missing;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string AudioPath
        {
            get => audioPath;
            set
            {
                UpdateFilePath(value);
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int Samples
        {
            get => samples;
            set => SetProperty(ref samples, value);
        }

        [XmlIgnore]
        public int SampleRate
        {
            get => sampleRate;
            set => SetProperty(ref sampleRate, value);
        }

        [XmlIgnore]
        public TimeSpan Length
        {
            get => length;
            set => SetProperty(ref length, value);
        }

        public string FileName => string.IsNullOrWhiteSpace(AudioPath) ? string.Empty : new FileInfo(AudioPath).Name;

        [XmlIgnore]
        public long Size
        {
            get => size;
            set => SetProperty(ref size, value);
        }

        [XmlIgnore]
        public string FormatStatus
        {
            get => formatStatus;
            set => SetProperty(ref formatStatus, value);
        }

        [XmlIgnore]
        public bool NeedsConversion
        {
            get => needsConversion;
            set => SetProperty(ref needsConversion, value);
        }

        [XmlIgnore]
        public SoundFormatState FormatState
        {
            get => formatState;
            set => SetProperty(ref formatState, value);
        }

        public Sound() { }

        public Sound(string filePath)
        {
            AudioPath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath).ToLower().Replace(" ", "_");
        }

        public void RefreshMetadata(WavFormatAnalyzer? analyzer = null)
        {
            UpdateFilePath(audioPath, analyzer);
        }

        private void UpdateFilePath(string filePath, WavFormatAnalyzer? analyzer = null)
        {
            audioPath = filePath;
            NeedsConversion = false;
            FormatStatus = string.Empty;
            FormatState = SoundFormatState.Missing;

            if (!File.Exists(filePath))
            {
                FormatStatus = "No file";
                return;
            }

            using (var wfr = new WaveFileReader(filePath))
            {
                Length = wfr.TotalTime;
                SampleRate = wfr.WaveFormat.SampleRate;
                var len = (int)wfr.Length;
                var other = wfr.WaveFormat.Channels * wfr.WaveFormat.BitsPerSample / 8;
                Samples = other > 0 ? len / other : 0;
            }

            Size = new FileInfo(filePath).Length;

            analyzer ??= new WavFormatAnalyzer();
            if (analyzer.TryAnalyze(filePath, out var info, out _))
            {
                if (info!.IsCompatible)
                {
                    FormatStatus = "OK";
                    FormatState = SoundFormatState.Ok;
                }
                else
                {
                    NeedsConversion = true;
                    FormatStatus = info.GetIssuesSummary();
                    FormatState = SoundFormatState.NeedsConversion;
                }
            }
            else
            {
                FormatStatus = "Unreadable WAV";
            }
        }
    }
}
