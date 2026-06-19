using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SirenSharp.Validators;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SirenSharp.ViewModels
{
    public class NewProjectViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly ErrorsViewModel errorsViewModel = new();
        private string projectName = "SirenSharpProject1";
        private string projectPath = string.Empty;

        public string ProjectName
        {
            get => projectName;
            set
            {
                SetProperty(ref projectName, value);
                Validate(new ProjectNameValidator().ValidateValue(projectName));
            }
        }

        public string ProjectPath
        {
            get => projectPath;
            set => SetProperty(ref projectPath, value);
        }

        public bool CanCreate => !HasErrors;

        public ICommand BrowseCommand { get; }
        public RelayCommand<Window> CreateCommand { get; }
        public RelayCommand<Window> CancelCommand { get; }

        public bool HasErrors => errorsViewModel.HasErrors;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public NewProjectViewModel()
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
            var sirenSharpFolder = Path.Combine(localAppDataPath, "SirenSharp", "Projects");
            Directory.CreateDirectory(sirenSharpFolder);
            ProjectPath = sirenSharpFolder;
        }

        private void Validate(System.Windows.Controls.ValidationResult result)
        {
            errorsViewModel.ClearErrors();
            if (!result.IsValid)
                errorsViewModel.AddError(result.ErrorContent?.ToString() ?? "Invalid");
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(ProjectName)));
            OnPropertyChanged(nameof(CanCreate));
        }

        private void Browse()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select project location",
                InitialDirectory = Directory.Exists(ProjectPath) ? ProjectPath : string.Empty
            };

            if (dialog.ShowDialog() == true)
                ProjectPath = dialog.FolderName;
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
