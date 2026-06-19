using System.Diagnostics;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class HelpWindow : FluentWindow
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }
    }
}
