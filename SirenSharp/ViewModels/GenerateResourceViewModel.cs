using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SirenSharp.Validators;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SirenSharp.ViewModels
{
    public class GenerateResourceViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ErrorsViewModel errorsViewModel = new();
        private string resourceName = "my-new-resource";
        private string dlcName = string.Empty;
        private string resourcePath = string.Empty;
        private string fxVersion = "cerulean";
        private bool generateTester = true;
        private bool useNativeBackend;

        public string ResourceName
        {
            get => resourceName;
            set
            {
                SetProperty(ref resourceName, value);
                Validate(nameof(ResourceName), new ResourceNameValidator().ValidateValue(resourceName));
            }
        }

        public string DlcName
        {
            get => dlcName;
            set
            {
                SetProperty(ref dlcName, value);
                Validate(nameof(DlcName), new DLCNameValidator().ValidateValue(dlcName));
            }
        }

        public string ResourcePath
        {
            get => resourcePath;
            set => SetProperty(ref resourcePath, value);
        }

        public string FxVersion
        {
            get => fxVersion;
            set => SetProperty(ref fxVersion, value);
        }

        public bool GenerateTester
        {
            get => generateTester;
            set => SetProperty(ref generateTester, value);
        }

        public bool UseNativeBackend
        {
            get => useNativeBackend;
            set => SetProperty(ref useNativeBackend, value);
        }

        public string[] FxVersionOptions { get; } = { "cerulean", "bodacious", "adamant" };

        public bool CanCreate => !HasErrors;

        public ICommand BrowseCommand { get; }
        public RelayCommand<Window> CreateCommand { get; }
        public RelayCommand<Window> CancelCommand { get; }

        public bool HasErrors => errorsViewModel.HasErrors;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public GenerateResourceViewModel()
        {
            BrowseCommand = new RelayCommand(Browse);
            CreateCommand = new RelayCommand<Window>(Create);
            CancelCommand = new RelayCommand<Window>(Cancel);

            errorsViewModel.ErrorsChanged += (_, e) =>
            {
                ErrorsChanged?.Invoke(this, e);
                OnPropertyChanged(nameof(CanCreate));
            };

            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var sirenSharpFolder = Path.Combine(localAppDataPath, "SirenSharp");
            Directory.CreateDirectory(sirenSharpFolder);
            ResourcePath = Path.Combine(sirenSharpFolder, "Resources");
        }

        private void Validate(string propertyName, System.Windows.Controls.ValidationResult result)
        {
            errorsViewModel.ClearErrors();
            if (!result.IsValid)
                errorsViewModel.AddError(result.ErrorContent?.ToString() ?? "Invalid");
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(CanCreate));
        }

        private void Browse()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select output location",
                InitialDirectory = Directory.Exists(ResourcePath) ? ResourcePath : string.Empty
            };

            if (dialog.ShowDialog() == true)
                ResourcePath = dialog.FolderName;
        }

        private static void Create(Window? window)
        {
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private static void Cancel(Window? window)
        {
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        public IEnumerable GetErrors(string? propertyName) => errorsViewModel.GetErrors(propertyName);
    }
}
