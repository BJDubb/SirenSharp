using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using SirenSharp.ViewModels;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public partial class OpenProjectWindow : FluentWindow
    {
        public IReadOnlyList<RecentProjectInfo> Recents { get; }

        public string? SelectedPath { get; private set; }

        public OpenProjectWindow(IEnumerable<RecentProjectInfo> recents)
        {
            Recents = new List<RecentProjectInfo>(recents);
            DataContext = this;
            InitializeComponent();
        }

        private void OnDoubleClick(object sender, RoutedEventArgs e)
        {
            if (RecentList.SelectedItem is RecentProjectInfo info)
                Accept(info.Path);
        }

        private void OnOpen(object sender, RoutedEventArgs e)
        {
            if (RecentList.SelectedItem is RecentProjectInfo info)
            {
                Accept(info.Path);
                return;
            }

            MessageDialog.Info("Open Project", "Select a recent project, or use Browse to pick a .ssproj file.");
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "SirenSharp Project (*.ssproj)|*.ssproj",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true)
                Accept(ofd.FileName);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Accept(string path)
        {
            SelectedPath = path;
            DialogResult = true;
            Close();
        }
    }
}
