using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class ProgressWindow : FluentWindow
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string status)
        {
            StatusText.Text = status;
        }
    }
}
