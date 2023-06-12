using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using SirenSharp.Models;
using SirenSharp.Views;
using System;
using System.Linq;
using SirenSharp.Converters;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using SirenSharp.Validators;
using System.Windows.Media;
using SirenSharp.Services;

namespace SirenSharp.ViewModels
{
    public class MainViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        public ICommand NewProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand SaveProjectAsCommand { get; }
        public ICommand NewAwcCommand { get; }
        public ICommand NewSirenCommand { get; }
        public ICommand ImportWavCommand { get; }
        public ICommand DeleteAwcCommand { get; }
        public ICommand DeleteSirenCommand { get; }
        public ICommand BrowseSirenCommand { get; }
        public ICommand GenerateResourceCommand { get; }
        public ICommand OpenAboutCommand { get; }

        private ErrorsViewModel errorsViewModel;
        private Project project;
        public Project Project
        {
            get => project;
            set
            {
                project = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(CanGenerateResource));
            }
        }

        private SoundSet currentSoundSet;
        public SoundSet CurrentSoundSet
        {
            get { return currentSoundSet; }
            set
            {
                currentSoundSet = value;
                CurrentSoundSetName = value?.Name;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SoundSetInfoText));
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(CanGenerateResource));
            }
        }

        private string currentSoundSetName;
        public string CurrentSoundSetName
        {
            get { return currentSoundSetName; }
            set
            {
                currentSoundSetName = value;
                if (CurrentSoundSet is not null)
                    CurrentSoundSet.Name = value;
                errorsViewModel.ClearErrors();

                OnPropertyChanged(nameof(CurrentSoundSet));
                OnPropertyChanged();
                var validResult = new AwcNameValidator().Validate(CurrentSoundSetName, null);
                if (!validResult.IsValid)
                {
                    errorsViewModel.AddError(validResult.ErrorContent.ToString());
                }
                OnPropertyChanged(nameof(CanGenerateResource));
            }
        }


        private Sound currentSiren;
        public Sound CurrentSiren
        {
            get { return currentSiren; }
            set
            {
                currentSiren = value;
                CurrentSirenName = value?.Name;
                CurrentSirenPath = value?.AudioPath;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(CanGenerateResource));
            }
        }

        private string currentSirenName;
        public string CurrentSirenName
        {
            get => currentSirenName;
            set
            {
                currentSirenName = value;
                if (CurrentSiren is not null)
                    CurrentSiren.Name = value;
                errorsViewModel.ClearErrors();

                OnPropertyChanged(nameof(CurrentSiren));
                OnPropertyChanged();
                var validResult = new SirenNameValidator().Validate(CurrentSirenName, null);
                if (!validResult.IsValid)
                {
                    errorsViewModel.AddError(validResult.ErrorContent.ToString());
                }
                OnPropertyChanged(nameof(CanGenerateResource));
            }
        }
        private string currentSirenPath;
        public string CurrentSirenPath
        {
            get => currentSirenPath;
            set
            {
                currentSirenPath = value;
                
                errorsViewModel.ClearErrors();


                var validResult = new AudioFileValidator().Validate(CurrentSirenPath, null);
                if (!validResult.IsValid)
                {
                    errorsViewModel.AddError(validResult.ErrorContent.ToString());
                }
                else
                {
                    if (CurrentSiren is not null)
                        CurrentSiren.AudioPath = value;
                }
                
                OnPropertyChanged(nameof(CurrentSiren));
                OnPropertyChanged(nameof(CanGenerateResource));
                OnPropertyChanged();
            }
        }


        public string SoundSetInfoText
        {
            get => GetSoundSetInfoText();
        }

        public string WindowTitle
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"SirenSharp - v{version.Major}.{version.Minor}" + (project != null ? $" [{project.ProjectName}.ssproj{(project.HasUnsavedChanges() ? "*" : "")}]" : "");
            }
        }

        private string statusBarText;
        private readonly IResourceGenerator resourceGenerator;

        public string StatusBarText
        {
            get => statusBarText;
            set
            {
                statusBarText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusBarColor));
            }
        }

        public Brush StatusBarColor
        {
            get
            {
                if (Project is null) return new SolidColorBrush(Colors.Black);
                return Project.IsValid() ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.Red);
            }
        }

        public bool CanGenerateResource
        {
            get  
            {
                StatusBarText = Project?.GetErrors().FirstOrDefault("Ready");
                return Project != null && Project.IsValid() && Project.SoundSets.Count > 0; 
            }
        }

        public bool HasErrors => errorsViewModel.HasErrors;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public MainViewModel(IResourceGenerator resourceGenerator)
        {
            NewProjectCommand = new RelayCommand(NewProject);
            OpenProjectCommand = new RelayCommand(OpenProject);
            SaveProjectCommand = new RelayCommand(SaveProject);
            SaveProjectAsCommand = new RelayCommand(SaveProjectAs);
            NewAwcCommand = new RelayCommand(NewAwc);
            NewSirenCommand = new RelayCommand(NewSiren);
            ImportWavCommand = new RelayCommand(ImportWav);
            DeleteAwcCommand = new RelayCommand(DeleteAwc);
            DeleteSirenCommand = new RelayCommand(DeleteSiren);
            BrowseSirenCommand = new RelayCommand(BrowseSiren);
            GenerateResourceCommand = new RelayCommand(GenerateResource);
            OpenAboutCommand = new RelayCommand(() => new AboutWindow().Show());

            errorsViewModel = new ErrorsViewModel();
            errorsViewModel.ErrorsChanged += (s, e) =>
            {
                ErrorsChanged?.Invoke(this, e);
                OnPropertyChanged(nameof(CanGenerateResource));
                OnPropertyChanged(nameof(StatusBarText));
            };

            this.resourceGenerator = resourceGenerator;
        }

        private void NewProject()
        {
            if (Project != null)
            {
                var result = MessageBox.Show("This project has unsaved changes. Are you sure you want continue?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            var newProjectViewModel = new NewProjectViewModel();
            var newProjectDialog = new NewProjectWindow
            {
                DataContext = newProjectViewModel
            };

            if (newProjectDialog.ShowDialog() == false)
            {
                return;
            }

            var projectName = newProjectViewModel.ProjectName;
            var projectPath = Path.Combine(newProjectViewModel.ProjectPath, projectName + ".ssproj");

            if (!Directory.Exists(newProjectViewModel.ProjectPath))
            {
                Directory.CreateDirectory(newProjectViewModel.ProjectPath);
            }


            Project = new Project(projectName, projectPath);
        }

        private void OpenProject()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "SirenSharp Project (*.ssproj)|*.ssproj";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true)
            {
                if (File.Exists(ofd.FileName))
                {
                    Project = Project.Load(ofd.FileName);
                }
                else
                {
                    MessageBox.Show("Invalid file selected!", "Error Reading File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveProject()
        {
            if (Project != null)
            {
                Project.Save();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        private void SaveProjectAs()
        {
            if (Project == null) return;
            var sfd = new SaveFileDialog();
            sfd.RestoreDirectory = true;
            sfd.Title = $"Save {Project.ProjectName}";
            sfd.FileName = $"{Project.ProjectName}.ssproj";
            sfd.Filter = "SirenSharp Project (*.ssproj)|*.ssproj";

            if (sfd.ShowDialog() == true)
            {
                string? filepath = sfd.FileName;
                Project.SaveAs(filepath);
            }
        }

        private void NewAwc()
        {
            var newSoundSet = new SoundSet("new_soundset");
            Project.SoundSets.Add(newSoundSet);
            OnPropertyChanged(nameof(CanGenerateResource));
        }

        private void NewSiren()
        {
            if (CurrentSoundSet != null)
            {
                var newSound = new Sound { Name = "new_siren" };
                CurrentSoundSet.AddSound(newSound);
            }
            OnPropertyChanged(nameof(CanGenerateResource));
        }

        private void ImportWav()
        {
            if (CurrentSoundSet == null) return;
            
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave File (*.wav)|*.wav";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;
            ofd.Title = "Select multiple .wav files";

            if (ofd.ShowDialog() == true)
            {
                foreach (var fileName in ofd.FileNames)
                {
                    if (!File.Exists(fileName)) return;

                    var newSound = new Sound(fileName);
                    CurrentSoundSet.AddSound(newSound);
                }
            }

            OnPropertyChanged(nameof(CanGenerateResource));
        }

        private void DeleteAwc()
        {
            if (CurrentSoundSet != null)
            {
                project.SoundSets.Remove(CurrentSoundSet);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(CurrentSoundSetName)));
            }
            OnPropertyChanged(nameof(CanGenerateResource));
        }

        private void DeleteSiren()
        {
            if (CurrentSoundSet != null && CurrentSiren != null)
            {
                CurrentSoundSet.Sounds.Remove(CurrentSiren);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(CurrentSirenName)));
            }
            OnPropertyChanged(nameof(CanGenerateResource));
        }

        private void BrowseSiren()
        {
            if (CurrentSiren == null) return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wave File (*.wav)|*.wav";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true)
            {
                if (File.Exists(ofd.FileName))
                {
                    CurrentSirenPath = ofd.FileName;
                }
                else
                {
                    MessageBox.Show("Invalid file selected!", "Error Reading File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateResource()
        {
            var generateResourceViewModel = new GenerateResourceViewModel();
            var generateResourceWindow = new GenerateResourceWindow
            {
                DataContext = generateResourceViewModel
            };

            if (generateResourceWindow.ShowDialog() == false)
            {
                return;
            }

            var resourceDir = Path.Combine(generateResourceViewModel.ResourcePath, generateResourceViewModel.ResourceName);

            if (Directory.Exists(resourceDir))
            {
                var result = MessageBox.Show($"A resource with the name \"{generateResourceViewModel.ResourceName}\" already exists in this directory.\n\nDo you want to replace it?", "Resource Already Exists", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
                Directory.Delete(resourceDir, true);
            }


            StatusBarText = "Generating Resource...";

            resourceGenerator.GenerateResource(generateResourceViewModel.ResourceName, generateResourceViewModel.DlcName, generateResourceViewModel.ResourcePath, Project.SoundSets.ToList());

            StatusBarText = "Resource generated successfully.";
        }

        private string GetSoundSetInfoText()
        {
            var totalTracksLength = new TimeSpan(currentSoundSet?.Sounds?.Sum(x => x.Length.Ticks) ?? 0L);
            var totalTracksSize = currentSoundSet?.Sounds?.Sum(x => !File.Exists(x.AudioPath) ? 0 : new FileInfo(x.AudioPath)?.Length) ?? 0;
            return $"{currentSoundSet?.Sounds.Count ?? 0} track(s), Length: {totalTracksLength:mm\\:ss}, Size: {FileSizeConverter.HumanReadableBytes(totalTracksSize)}";
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            return errorsViewModel.GetErrors(propertyName);
        }
    }
}
