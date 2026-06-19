using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace SirenSharp.Views
{
    public enum MessageDialogKind
    {
        Info,
        Success,
        Warning,
        Error,
        Question,
    }

    public enum MessageDialogChoice
    {
        Primary,
        Secondary,
        Cancel,
    }

    public partial class MessageDialog : FluentWindow
    {
        public MessageDialog()
        {
            InitializeComponent();
        }

        // Defaults to Cancel so dismissing via the title-bar close button is treated
        // as the safe "don't do anything" choice.
        public MessageDialogChoice Choice { get; private set; } = MessageDialogChoice.Cancel;

        private void OnPrimary(object sender, RoutedEventArgs e)
        {
            Choice = MessageDialogChoice.Primary;
            DialogResult = true;
            Close();
        }

        private void OnSecondary(object sender, RoutedEventArgs e)
        {
            Choice = MessageDialogChoice.Secondary;
            DialogResult = false;
            Close();
        }

        private void OnTertiary(object sender, RoutedEventArgs e)
        {
            Choice = MessageDialogChoice.Cancel;
            DialogResult = false;
            Close();
        }

        private void Configure(string title, string message, MessageDialogKind kind,
            string primaryText, string? secondaryText, string? tertiaryText = null)
        {
            Title = title;
            TitleBarControl.Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
            PrimaryButton.Content = primaryText;

            if (secondaryText == null)
            {
                SecondaryButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                SecondaryButton.Content = secondaryText;
            }

            if (tertiaryText != null)
            {
                TertiaryButton.Content = tertiaryText;
                TertiaryButton.Visibility = Visibility.Visible;
            }

            var (symbol, brushKey) = kind switch
            {
                MessageDialogKind.Success => (SymbolRegular.CheckmarkCircle24, "SystemFillColorSuccessBrush"),
                MessageDialogKind.Warning => (SymbolRegular.Warning24, "SystemFillColorCautionBrush"),
                MessageDialogKind.Error => (SymbolRegular.ErrorCircle24, "SystemFillColorCriticalBrush"),
                MessageDialogKind.Question => (SymbolRegular.QuestionCircle24, "AccentTextFillColorPrimaryBrush"),
                _ => (SymbolRegular.Info24, "AccentTextFillColorPrimaryBrush"),
            };

            HeaderIcon.Symbol = symbol;
            HeaderIcon.Foreground = (Brush)FindResource(brushKey);
        }

        private static Window? ResolveOwner()
        {
            var main = Application.Current?.MainWindow;
            return (main != null && main.IsLoaded) ? main : null;
        }

        public static bool Confirm(string title, string message, string yesText = "Yes", string noText = "Cancel",
            MessageDialogKind kind = MessageDialogKind.Question)
        {
            var dlg = new MessageDialog { Owner = ResolveOwner() };
            dlg.Configure(title, message, kind, yesText, noText);
            return dlg.ShowDialog() == true;
        }

        public static MessageDialogChoice ThreeChoice(string title, string message,
            string primaryText, string secondaryText, string cancelText,
            MessageDialogKind kind = MessageDialogKind.Warning)
        {
            var dlg = new MessageDialog { Owner = ResolveOwner() };
            dlg.Configure(title, message, kind, primaryText, secondaryText, cancelText);
            dlg.ShowDialog();
            return dlg.Choice;
        }

        public static void Info(string title, string message, MessageDialogKind kind = MessageDialogKind.Info)
        {
            var dlg = new MessageDialog { Owner = ResolveOwner() };
            dlg.Configure(title, message, kind, "OK", null);
            dlg.ShowDialog();
        }

        public static void Error(string title, string message) => Info(title, message, MessageDialogKind.Error);

        public static void Warn(string title, string message) => Info(title, message, MessageDialogKind.Warning);
    }
}
