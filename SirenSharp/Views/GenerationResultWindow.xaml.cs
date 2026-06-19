using System.Windows;
using System.Windows.Media;
using SirenSharp.ViewModels;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class GenerationResultWindow : FluentWindow
    {
        public GenerationResultWindow()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (vm.LastGenerationSucceeded)
            {
                HeaderIcon.Symbol = SymbolRegular.CheckmarkCircle24;
                HeaderIcon.Foreground = (Brush)FindResource("SystemFillColorSuccessBrush");
            }
            else
            {
                HeaderIcon.Symbol = SymbolRegular.ErrorCircle24;
                HeaderIcon.Foreground = (Brush)FindResource("SystemFillColorCriticalBrush");
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
