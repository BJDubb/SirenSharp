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
        private double trimStartSeconds;
        private double trimEndSeconds;

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

        /// <summary>Trim in-point in seconds from the start of the source. 0 = from the beginning.</summary>
        public double TrimStartSeconds
        {
            get => trimStartSeconds;
            set
            {
                if (SetProperty(ref trimStartSeconds, value))
                {
                    OnPropertyChanged(nameof(IsTrimmed));
                    OnPropertyChanged(nameof(TrimDisplayText));
                }
            }
        }

        /// <summary>Trim out-point in seconds. 0 (or past the end) = play to the end.</summary>
        public double TrimEndSeconds
        {
            get => trimEndSeconds;
            set
            {
                if (SetProperty(ref trimEndSeconds, value))
                {
                    OnPropertyChanged(nameof(IsTrimmed));
                    OnPropertyChanged(nameof(TrimDisplayText));
                }
            }
        }

        [XmlIgnore]
        public bool IsTrimmed => TrimStartSeconds > 0 || TrimEndSeconds > 0;

        /// <summary>Human-readable trim range; an unset out-point reads "end" rather than 0.00s.</summary>
        [XmlIgnore]
        public string TrimDisplayText =>
            $"{TrimStartSeconds:0.00}s – {(TrimEndSeconds > TrimStartSeconds ? $"{TrimEndSeconds:0.00}s" : "end")}";

        /// <summary>Scratch filename for the sanitized WAV the build pipeline writes and reads,
        /// regardless of the source extension (MP3/OGG sources still become a .wav here).</summary>
        [XmlIgnore]
        public string PreparedFileName => $"{Name}.wav";

        [XmlIgnore]
        public double LengthSeconds => length.TotalSeconds;

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
            set
            {
                if (SetProperty(ref length, value))
                    OnPropertyChanged(nameof(LengthSeconds));
            }
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

            if (!AudioReaderFactory.IsSupported(filePath))
            {
                FormatStatus = "Unsupported file type";
                return;
            }

            using (var wfr = AudioReaderFactory.Open(filePath))
            {
                Length = wfr.TotalTime;
                SampleRate = wfr.WaveFormat.SampleRate;
                var len = (int)wfr.Length;
                var other = wfr.WaveFormat.Channels * wfr.WaveFormat.BitsPerSample / 8;
                Samples = other > 0 ? len / other : 0;
            }

            // Clamp any trim carried over from a previous source so stale in/out points
            // can't sit past the end of the new clip.
            var clipSeconds = length.TotalSeconds;
            if (clipSeconds > 0)
            {
                if (trimStartSeconds >= clipSeconds) TrimStartSeconds = 0;
                if (trimEndSeconds > clipSeconds) TrimEndSeconds = 0;
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
