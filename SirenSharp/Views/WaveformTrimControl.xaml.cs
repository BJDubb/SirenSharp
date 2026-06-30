using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NAudio.Wave;
using SirenSharp.Services;

namespace SirenSharp.Views
{
    /// <summary>
    /// Draws a clip's waveform and lets the user drag in/out handles to set the trim points.
    /// Binds two-way to TrimStartSeconds/TrimEndSeconds (0 end = "to the end of the clip").
    /// </summary>
    public partial class WaveformTrimControl : UserControl
    {
        private float[] peaks = System.Array.Empty<float>(); // mono abs-peak per source frame

        public WaveformTrimControl() => InitializeComponent();

        public static readonly DependencyProperty AudioPathProperty = DependencyProperty.Register(
            nameof(AudioPath), typeof(string), typeof(WaveformTrimControl),
            new PropertyMetadata(null, (d, _) => ((WaveformTrimControl)d).LoadPeaks()));

        public static readonly DependencyProperty DurationSecondsProperty = DependencyProperty.Register(
            nameof(DurationSeconds), typeof(double), typeof(WaveformTrimControl),
            new PropertyMetadata(0.0, (d, _) => ((WaveformTrimControl)d).Redraw()));

        public static readonly DependencyProperty TrimStartSecondsProperty = DependencyProperty.Register(
            nameof(TrimStartSeconds), typeof(double), typeof(WaveformTrimControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((WaveformTrimControl)d).UpdateHandles()));

        public static readonly DependencyProperty TrimEndSecondsProperty = DependencyProperty.Register(
            nameof(TrimEndSeconds), typeof(double), typeof(WaveformTrimControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((WaveformTrimControl)d).UpdateHandles()));

        public string? AudioPath { get => (string?)GetValue(AudioPathProperty); set => SetValue(AudioPathProperty, value); }
        public double DurationSeconds { get => (double)GetValue(DurationSecondsProperty); set => SetValue(DurationSecondsProperty, value); }
        public double TrimStartSeconds { get => (double)GetValue(TrimStartSecondsProperty); set => SetValue(TrimStartSecondsProperty, value); }
        public double TrimEndSeconds { get => (double)GetValue(TrimEndSecondsProperty); set => SetValue(TrimEndSecondsProperty, value); }

        private double EffectiveEnd => TrimEndSeconds > 0 && TrimEndSeconds <= DurationSeconds ? TrimEndSeconds : DurationSeconds;

        private async void LoadPeaks()
        {
            var path = AudioPath;
            peaks = System.Array.Empty<float>();

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                Redraw();
                return;
            }

            try
            {
                peaks = await System.Threading.Tasks.Task.Run(() => ReadPeaks(path));
            }
            catch
            {
                peaks = System.Array.Empty<float>();
            }
            Redraw();
        }

        private static float[] ReadPeaks(string path)
        {
            using var reader = AudioReaderFactory.Open(path);
            var sp = reader.ToSampleProvider();
            var channels = reader.WaveFormat.Channels;

            var result = new System.Collections.Generic.List<float>();
            var buffer = new float[reader.WaveFormat.SampleRate * channels];
            int read;
            while ((read = sp.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i + channels <= read; i += channels)
                {
                    float peak = 0;
                    for (int c = 0; c < channels; c++) peak = System.Math.Max(peak, System.Math.Abs(buffer[i + c]));
                    result.Add(peak);
                }
            }
            return result.ToArray();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        private void Redraw()
        {
            DrawWave();
            UpdateHandles();
        }

        private void DrawWave()
        {
            var w = Host.ActualWidth;
            var h = Host.ActualHeight;
            if (w < 1 || h < 1 || peaks.Length == 0)
            {
                WavePath.Data = null;
                return;
            }

            var mid = h / 2;
            var columns = (int)w;
            var perCol = (double)peaks.Length / columns;
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                for (int x = 0; x < columns; x++)
                {
                    var from = (int)(x * perCol);
                    var to = System.Math.Min(peaks.Length, (int)((x + 1) * perCol));
                    float p = 0;
                    for (int i = from; i < to; i++) p = System.Math.Max(p, peaks[i]);
                    var half = p * (mid - 2);
                    ctx.BeginFigure(new Point(x, mid - half), false, false);
                    ctx.LineTo(new Point(x, mid + half), true, false);
                }
            }
            geo.Freeze();
            WavePath.Data = geo;
        }

        private void UpdateHandles()
        {
            var w = Host.ActualWidth;
            var h = Host.ActualHeight;
            if (w < 1 || DurationSeconds <= 0) return;

            var pxPerSec = w / DurationSeconds;
            var startX = TrimStartSeconds * pxPerSec;
            var endX = EffectiveEnd * pxPerSec;

            StartThumb.Height = h;
            EndThumb.Height = h;
            Canvas.SetLeft(StartThumb, startX - StartThumb.Width / 2);
            Canvas.SetLeft(EndThumb, endX - EndThumb.Width / 2);

            LeftShade.Height = h;
            LeftShade.Width = System.Math.Max(0, startX);
            RightShade.Height = h;
            Canvas.SetLeft(RightShade, endX);
            RightShade.Width = System.Math.Max(0, w - endX);
        }

        private void StartThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (Host.ActualWidth < 1 || DurationSeconds <= 0) return;
            var deltaSec = e.HorizontalChange / Host.ActualWidth * DurationSeconds;
            var newStart = Clamp(TrimStartSeconds + deltaSec, 0, EffectiveEnd - 0.02);
            TrimStartSeconds = newStart;
        }

        private void EndThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (Host.ActualWidth < 1 || DurationSeconds <= 0) return;
            var deltaSec = e.HorizontalChange / Host.ActualWidth * DurationSeconds;
            var newEnd = Clamp(EffectiveEnd + deltaSec, TrimStartSeconds + 0.02, DurationSeconds);
            // Snap to "no end trim" when dragged to the far right.
            TrimEndSeconds = newEnd >= DurationSeconds - 0.02 ? 0 : newEnd;
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
