using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using SirenSharp.Services;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class AboutWindow : FluentWindow
    {
        private readonly UpdateService? updateService;

        public AboutWindow(UpdateService? updateService = null)
        {
            InitializeComponent();
            this.updateService = updateService;

            if (updateService != null)
            {
                versionText.Text = $"Version: v{updateService.CurrentVersion} ({updateService.Channel})";
            }
            else
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                versionText.Text = $"Version: v{version?.Major ?? 0}.{version?.Minor ?? 0}";
            }
        }

        private async void OnCheckForUpdates(object sender, RoutedEventArgs e)
        {
            if (updateService == null) return;

            if (!updateService.IsInstalled)
            {
                updateStatus.Text = "Updates apply to the installer build only.";
                return;
            }

            checkButton.IsEnabled = false;
            updateStatus.Text = "Checking...";
            try
            {
                var version = await updateService.CheckForUpdatesAsync();
                if (version == null)
                {
                    updateStatus.Text = "You're on the latest version.";
                }
                else
                {
                    updateStatus.Text = $"Downloading v{version}...";
                    await updateService.DownloadAndApplyAsync();
                }
            }
            catch (System.Exception ex)
            {
                updateStatus.Text = $"Update check failed: {ex.Message}";
            }
            finally
            {
                checkButton.IsEnabled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }
    }
}
