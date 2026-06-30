using SirenSharp.ViewModels;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += OnClosing;
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel != null && !ViewModel.ConfirmShutdown())
                e.Cancel = true;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnResetTrim(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.CurrentSiren is { } siren)
            {
                siren.TrimStartSeconds = 0;
                siren.TrimEndSeconds = 0;
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
            var audioFiles = files.Where(SirenSharp.Services.AudioReaderFactory.IsSupported).ToArray();
            if (audioFiles.Length == 0) return;
            ViewModel?.ImportWavFiles(audioFiles);
        }
    }
}
