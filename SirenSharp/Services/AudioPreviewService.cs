using NAudio.Wave;

namespace SirenSharp.Services
{
    public class AudioPreviewService : IDisposable
    {
        private WaveOutEvent? waveOut;
        private IDisposable? currentProvider;

        public bool IsPlaying => waveOut?.PlaybackState == PlaybackState.Playing;
        public string? CurrentSource { get; private set; }

        public void PlayWav(string filePath)
        {
            Stop();

            if (!File.Exists(filePath)) return;

            var reader = new AudioFileReader(filePath);
            currentProvider = reader;
            CurrentSource = filePath;

            waveOut = new WaveOutEvent();
            waveOut.Init(reader);
            waveOut.PlaybackStopped += (_, _) => Stop();
            waveOut.Play();
        }

        public void Stop()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            currentProvider?.Dispose();
            currentProvider = null;

            CurrentSource = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
