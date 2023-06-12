using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using SirenSharp.Validators;

namespace SirenSharp.ViewModels
{
    public class NewProjectViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private string projectName = string.Empty;
        private string projectPath;

        public string ProjectName
        {
            get => projectName;
            set
            {
                projectName = value;

                errorsViewModel.ClearErrors();
                var validResult = new ProjectNameValidator().Validate(projectName, null);
                if (!validResult.IsValid)
                {
                    errorsViewModel.AddError(validResult.ErrorContent.ToString());
                }

                OnPropertyChanged();
            }
        }
        public string ProjectPath
        {
            get => projectPath; set
            {
                projectPath = value;
                OnPropertyChanged();
            }
        }
        public bool CanCreate => !HasErrors;

        public ICommand BrowseCommand { get; }
        public RelayCommand<Window> CreateCommand { get; }

        public bool HasErrors => errorsViewModel.HasErrors;

        private ErrorsViewModel errorsViewModel;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public NewProjectViewModel()
        {
            BrowseCommand = new RelayCommand(Browse);
            CreateCommand = new RelayCommand<Window>(Create);

            errorsViewModel = new ErrorsViewModel();
            errorsViewModel.ErrorsChanged += (s, e) =>
            {
                ErrorsChanged?.Invoke(this, e);
                OnPropertyChanged(nameof(CanCreate));
            };

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sirenSharpFolder = Path.Combine(localAppDataPath, "SirenSharp");
            Directory.CreateDirectory(sirenSharpFolder);

            ProjectName = "SirenSharpProject1";
            ProjectPath = Path.Combine(sirenSharpFolder, "Projects");
        }

        private void Browse()
        {
            var cfd = new CommonOpenFileDialog();
            cfd.IsFolderPicker = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ProjectPath = cfd.FileName;
            }

            if (!Directory.Exists(ProjectPath))
            {
                return;
            }
        }

        private void Create(Window window)
        {
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            return errorsViewModel.GetErrors(propertyName);
        }
    }
}
