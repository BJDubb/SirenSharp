using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Input;
using System.Windows;
using SirenSharp.Validators;
using System.ComponentModel;
using System.Collections;

namespace SirenSharp.ViewModels
{
    public class GenerateResourceViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private string resourceName;
        private string dlcName;
        private string resourcePath;

        public string ResourceName
        {
            get => resourceName;
            set
            {
                resourceName = value;

                errorsViewModel.ClearErrors();
                var validResult = new ResourceNameValidator().Validate(resourceName, null);
                if (!validResult.IsValid)
                {
                    errorsViewModel.AddError(validResult.ErrorContent.ToString());
                }
                OnPropertyChanged();
            }
        }

        public string DlcName
        {
            get => dlcName;
            set
            {
                dlcName = value;

                errorsViewModel.ClearErrors();
                var validResult = new DLCNameValidator().Validate(dlcName, null);
                if (!validResult.IsValid)
                {
                    errorsViewModel.AddError(validResult.ErrorContent.ToString());
                }
                OnPropertyChanged();
            }
        }
        public string ResourcePath
        {
            get => resourcePath; set
            {
                resourcePath = value;
                OnPropertyChanged();
            }
        }

        public bool CanCreate => !HasErrors;

        public ICommand BrowseCommand { get; }
        public RelayCommand<Window> CreateCommand { get; }
        public bool HasErrors => errorsViewModel.HasErrors;

        private ErrorsViewModel errorsViewModel;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public GenerateResourceViewModel()
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

            ResourceName = "my-new-resource";
            ResourcePath = Path.Combine(sirenSharpFolder, "Resources");
        }

        private void Browse()
        {
            var cfd = new CommonOpenFileDialog();
            cfd.IsFolderPicker = true;

            if (cfd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ResourcePath = cfd.FileName;
            }

            if (!Directory.Exists(ResourcePath))
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

