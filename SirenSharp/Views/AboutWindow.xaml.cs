using System.Diagnostics;
using System.Reflection;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class AboutWindow : FluentWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            versionText.Text = $"Version: v{version?.Major ?? 0}.{version?.Minor ?? 0}";
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }
    }
}
