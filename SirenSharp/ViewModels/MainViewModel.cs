using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SirenSharp.Models;
using SirenSharp.Services;
using SirenSharp.Services.Exporters;
using SirenSharp.Services.Preflight;
using SirenSharp.Validators;
using SirenSharp.Views;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SirenSharp.Converters;

namespace SirenSharp.ViewModels
{
    public class MainViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly IResourceExporter exporter;
        private readonly PreflightService preflight;
        private readonly DiagnosticsExporter diagnosticsExporter;
        private readonly AudioPreviewService audioPreview;
        private readonly WavSanitizer wavSanitizer;
        private readonly WavFormatAnalyzer wavFormatAnalyzer;
        private readonly ExternalToolLauncher externalTools;
        private readonly AppSettingsService appSettings;
        private readonly IServiceProvider serviceProvider;
        private readonly ErrorsViewModel errorsViewModel = new();

        private Project? project;
        private SoundSet? currentSoundSet;
        private string? currentSoundSetName;
        private Sound? currentSiren;
        private string? currentSirenName;
        private string? currentSirenPath;
        private string statusBarText = "Ready";
        private string? lastGeneratedResourcePath;
        private bool isGenerating;
        private DiagnosticReport preflightReport = new();
        private ResourceGenerationResult? lastGenerationResult;

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
        public ICommand OpenHelpCommand { get; }
        public ICommand ExportDiagnosticsCommand { get; }
        public ICommand PlaySirenCommand { get; }
        public ICommand StopPreviewCommand { get; }
        public ICommand FixSirenAudioCommand { get; }
        public ICommand OpenInRpfExplorerCommand { get; }
        public ICommand OpenRecentCommand { get; }
        public ICommand OpenOutputFolderCommand { get; }
        public IRelayCommand<Sound> PlaySoundCommand { get; }
        public IRelayCommand<Sound> FixSoundCommand { get; }
        public IRelayCommand<Sound> DeleteSoundCommand { get; }

        public ObservableCollection<RecentProjectInfo> RecentProjects { get; } = new();

        public MainViewModel(
            IResourceExporter exporter,
            PreflightService preflight,
            DiagnosticsExporter diagnosticsExporter,
            AudioPreviewService audioPreview,
            WavSanitizer wavSanitizer,
            WavFormatAnalyzer wavFormatAnalyzer,
            ExternalToolLauncher externalTools,
            AppSettingsService appSettings,
            IServiceProvider serviceProvider)
        {
            this.exporter = exporter;
            this.preflight = preflight;
            this.diagnosticsExporter = diagnosticsExporter;
            this.audioPreview = audioPreview;
            this.wavSanitizer = wavSanitizer;
            this.wavFormatAnalyzer = wavFormatAnalyzer;
            this.externalTools = externalTools;
            this.appSettings = appSettings;
            this.serviceProvider = serviceProvider;

            NewProjectCommand = new RelayCommand(NewProject);
            OpenProjectCommand = new RelayCommand(OpenProject);
            SaveProjectCommand = new RelayCommand(SaveProject, () => Project != null);
            SaveProjectAsCommand = new RelayCommand(SaveProjectAs, () => Project != null);
            NewAwcCommand = new RelayCommand(NewAwc, () => Project != null);
            NewSirenCommand = new RelayCommand(NewSiren, () => CurrentSoundSet != null);
            ImportWavCommand = new RelayCommand(ImportWav, () => CurrentSoundSet != null);
            DeleteAwcCommand = new RelayCommand(DeleteAwc, () => CurrentSoundSet != null);
            DeleteSirenCommand = new RelayCommand(DeleteSiren, () => CurrentSiren != null);
            BrowseSirenCommand = new RelayCommand(BrowseSiren, () => CurrentSiren != null);
            GenerateResourceCommand = new RelayCommand(GenerateResource, () => CanGenerateResource && !IsGenerating);
            OpenAboutCommand = new RelayCommand(() => new AboutWindow().Show());
            OpenHelpCommand = new RelayCommand(() => new HelpWindow().ShowDialog());
            ExportDiagnosticsCommand = new RelayCommand(ExportDiagnostics);
            PlaySirenCommand = new RelayCommand(PlaySiren, () => CurrentSiren != null && File.Exists(CurrentSiren.AudioPath));
            StopPreviewCommand = new RelayCommand(StopPreview, () => audioPreview.IsPlaying);
            FixSirenAudioCommand = new RelayCommand(FixSirenAudio, () => CurrentSiren != null && CurrentSiren.NeedsConversion);
            OpenInRpfExplorerCommand = new RelayCommand(OpenInRpfExplorer, () => !string.IsNullOrWhiteSpace(LastGeneratedResourcePath));
            OpenRecentCommand = new RelayCommand<string>(OpenRecent);
            OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, () => !string.IsNullOrWhiteSpace(LastGeneratedResourcePath));
            PlaySoundCommand = new RelayCommand<Sound>(PlaySound);
            FixSoundCommand = new RelayCommand<Sound>(FixSound);
            DeleteSoundCommand = new RelayCommand<Sound>(DeleteSound);

            errorsViewModel.ErrorsChanged += (_, e) =>
            {
                ErrorsChanged?.Invoke(this, e);
                RefreshValidationState();
            };

            LoadRecentProjects();
        }

        public Project? Project
        {
            get => project;
            set
            {
                SetProperty(ref project, value);
                OnPropertyChanged(nameof(HasProject));
                RefreshValidationState();
            }
        }

        public bool HasProject => Project != null;

        public SoundSet? CurrentSoundSet
        {
            get => currentSoundSet;
            set
            {
                SetProperty(ref currentSoundSet, value);
                CurrentSoundSetName = value?.Name;
                OnPropertyChanged(nameof(SoundSetInfoText));
                RefreshInspector();
                RefreshValidationState();
            }
        }

        public string? CurrentSoundSetName
        {
            get => currentSoundSetName;
            set
            {
                SetProperty(ref currentSoundSetName, value);
                if (CurrentSoundSet != null)
                    CurrentSoundSet.Name = value ?? string.Empty;

                ValidateField(nameof(CurrentSoundSetName), new AwcNameValidator().ValidateValue(CurrentSoundSetName));
            }
        }

        public Sound? CurrentSiren
        {
            get => currentSiren;
            set
            {
                SetProperty(ref currentSiren, value);
                CurrentSirenName = value?.Name;
                CurrentSirenPath = value?.AudioPath;
                RefreshInspector();
                RefreshValidationState();
            }
        }

        public bool ShowSirenInspector => CurrentSiren != null;
        public bool ShowSoundSetInspector => CurrentSiren == null && CurrentSoundSet != null;
        public bool ShowInspectorHint => CurrentSiren == null && CurrentSoundSet == null;

        public string? CurrentSirenName
        {
            get => currentSirenName;
            set
            {
                SetProperty(ref currentSirenName, value);
                if (CurrentSiren != null)
                    CurrentSiren.Name = value ?? string.Empty;

                ValidateField(nameof(CurrentSirenName), new SirenNameValidator().ValidateValue(CurrentSirenName));
            }
        }

        public string? CurrentSirenPath
        {
            get => currentSirenPath;
            set
            {
                SetProperty(ref currentSirenPath, value);

                if (CurrentSiren == null)
                {
                    RefreshValidationState();
                    return;
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    CurrentSiren.AudioPath = string.Empty;
                    CurrentSiren.RefreshMetadata(wavFormatAnalyzer);
                    RefreshValidationState();
                    return;
                }

                if (!File.Exists(value))
                {
                    ValidateField(nameof(CurrentSirenPath), new System.Windows.Controls.ValidationResult(false, "File does not exist"));
                    return;
                }

                errorsViewModel.ClearErrors();

                // Persist the path onto the model so it survives reselection/saving, and
                // re-analyze so the format badge/warning reflect the (possibly fixed) file.
                CurrentSiren.AudioPath = value;
                CurrentSiren.RefreshMetadata(wavFormatAnalyzer);

                StatusBarText = CurrentSiren.NeedsConversion
                    ? $"Format warning: {CurrentSiren.FormatStatus}. Use Fix Audio or generate (auto-converts)."
                    : "Ready";

                OnPropertyChanged(nameof(CurrentSiren));
                OnPropertyChanged(nameof(SoundSetInfoText));
                RefreshValidationState();
                OnPropertyChanged();
            }
        }

        public string SoundSetInfoText
        {
            get
            {
                var totalTracksLength = new TimeSpan(CurrentSoundSet?.Sounds?.Sum(x => x.Length.Ticks) ?? 0L);
                var totalTracksSize = CurrentSoundSet?.Sounds?.Sum(x => !File.Exists(x.AudioPath) ? 0 : new FileInfo(x.AudioPath).Length) ?? 0;
                return $"{CurrentSoundSet?.Sounds.Count ?? 0} track(s), Length: {totalTracksLength:mm\\:ss}, Size: {FileSizeConverter.HumanReadableBytes(totalTracksSize)}";
            }
        }

        public string WindowTitle
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var title = $"SirenSharp v{version?.Major}.{version?.Minor}";
                if (project != null)
                    title += $" - {project.ProjectName}.ssproj{(project.HasUnsavedChanges() ? "*" : "")}";
                return title;
            }
        }

        public string StatusBarText
        {
            get => statusBarText;
            set => SetProperty(ref statusBarText, value);
        }

        /// <summary>
        /// The latest preflight findings for the current project, recomputed by
        /// <see cref="RefreshValidationState"/>. Errors block generation; warnings don't.
        /// </summary>
        public DiagnosticReport PreflightReport
        {
            get => preflightReport;
            private set => SetProperty(ref preflightReport, value);
        }

        public bool HasValidationError => Project != null && PreflightReport.HasErrors;

        /// <summary>Short count line for the issues bar header, e.g. "2 errors · 1 warning".</summary>
        public string PreflightSummary
        {
            get
            {
                var errors = PreflightReport.Errors.Count();
                var warnings = PreflightReport.Warnings.Count();
                if (errors == 0 && warnings == 0) return "No issues";

                var parts = new List<string>();
                if (errors > 0) parts.Add($"{errors} error{(errors == 1 ? "" : "s")}");
                if (warnings > 0) parts.Add($"{warnings} warning{(warnings == 1 ? "" : "s")}");
                return string.Join(" · ", parts);
            }
        }

        // Live "unsaved changes" flag, surfaced in the toolbar and window title.
        public bool IsDirty => SafeHasUnsavedChanges();

        private bool SafeHasUnsavedChanges()
        {
            if (Project == null) return false;
            try { return Project.HasUnsavedChanges(); }
            catch { return true; }
        }

        public bool CanGenerateResource =>
            Project != null && !PreflightReport.HasErrors && Project.SoundSets.Count > 0;

        public bool IsGenerating
        {
            get => isGenerating;
            set
            {
                SetProperty(ref isGenerating, value);
                (GenerateResourceCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        public string? LastGeneratedResourcePath
        {
            get => lastGeneratedResourcePath;
            set
            {
                SetProperty(ref lastGeneratedResourcePath, value);
                (OpenInRpfExplorerCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (OpenOutputFolderCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        public bool LastGenerationSucceeded { get; private set; }
        public string LastGenerationTitle { get; private set; } = string.Empty;
        public string LastGenerationMessage { get; private set; } = string.Empty;
        public ObservableCollection<string> LastGenerationDetails { get; } = new();

        public bool HasErrors => errorsViewModel.HasErrors;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public void ImportWavFiles(IEnumerable<string> filePaths)
        {
            if (Project == null) return;

            // Make importing frictionless: if no soundset is selected, fall back to
            // the first one, or create a default soundset so dropped WAVs land somewhere.
            if (CurrentSoundSet == null)
            {
                if (Project.SoundSets.Count == 0)
                    Project.SoundSets.Add(new SoundSet("sirens"));
                CurrentSoundSet = Project.SoundSets[0];
            }

            var added = 0;
            foreach (var fileName in filePaths)
            {
                if (!File.Exists(fileName)) continue;
                var sound = new Sound(fileName);
                sound.RefreshMetadata(wavFormatAnalyzer);
                CurrentSoundSet!.AddSound(sound);
                added++;
            }

            if (added > 0)
                StatusBarText = $"Imported {added} WAV file{(added == 1 ? "" : "s")} into '{CurrentSoundSet!.Name}'.";

            OnPropertyChanged(nameof(SoundSetInfoText));
            RefreshValidationState();
        }

        private void RefreshInspector()
        {
            OnPropertyChanged(nameof(ShowSirenInspector));
            OnPropertyChanged(nameof(ShowSoundSetInspector));
            OnPropertyChanged(nameof(ShowInspectorHint));
        }

        private void NewProject()
        {
            if (!ConfirmDiscardUnsavedChanges()) return;

            var vm = serviceProvider.GetRequiredService<NewProjectViewModel>();
            var dialog = new NewProjectWindow { DataContext = vm };
            if (dialog.ShowDialog() != true) return;

            var projectPath = Path.Combine(vm.ProjectPath, vm.ProjectName + ".ssproj");

            if (File.Exists(projectPath))
            {
                if (!MessageDialog.Confirm("Project already exists",
                    $"A project named '{vm.ProjectName}' already exists in that folder. Overwrite it?",
                    "Overwrite", "Cancel", MessageDialogKind.Warning))
                    return;
            }

            Directory.CreateDirectory(vm.ProjectPath);
            Project = new Project(vm.ProjectName, projectPath);
            CurrentSoundSet = null;
            CurrentSiren = null;
            AddRecentProject(projectPath);
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void OpenProject()
        {
            if (!ConfirmDiscardUnsavedChanges()) return;

            var dialog = new OpenProjectWindow(RecentProjects) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;

            LoadProjectFromPath(dialog.SelectedPath);
        }

        private void OpenRecent(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!ConfirmDiscardUnsavedChanges()) return;
            LoadProjectFromPath(path);
        }

        private void LoadProjectFromPath(string path)
        {
            if (!File.Exists(path))
            {
                MessageDialog.Error("Project not found", "That project file could not be found. It may have been moved or deleted.");
                appSettings.Settings.RecentProjects.Remove(path);
                appSettings.Save();
                LoadRecentProjects();
                return;
            }

            try
            {
                var loaded = Project.Load(path);
                foreach (var soundSet in loaded.SoundSets)
                {
                    foreach (var sound in soundSet.Sounds)
                        sound.RefreshMetadata(wavFormatAnalyzer);
                }

                Project = loaded;
                CurrentSoundSet = Project.SoundSets.FirstOrDefault();
                CurrentSiren = null;
                AddRecentProject(path);
                OnPropertyChanged(nameof(WindowTitle));
                RefreshValidationState();
            }
            catch (Exception ex)
            {
                MessageDialog.Error("Open failed", $"Could not open the project:\n\n{ex.Message}");
            }
        }

        // Called when the window is closing. Returns true if the app may close.
        // Offers to save unsaved changes; lets the user cancel the shutdown.
        public bool ConfirmShutdown()
        {
            if (!SafeHasUnsavedChanges()) return true;

            var choice = MessageDialog.ThreeChoice("Unsaved changes",
                $"'{Project!.ProjectName}' has unsaved changes. Save before closing?",
                "Save", "Don't save", "Cancel", MessageDialogKind.Warning);

            switch (choice)
            {
                case MessageDialogChoice.Primary:
                    try
                    {
                        Project.Save();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageDialog.Error("Save failed", $"Could not save the project:\n\n{ex.Message}");
                        return false; // keep the app open so the user doesn't lose work
                    }
                case MessageDialogChoice.Secondary:
                    return true; // discard
                default:
                    return false; // cancel close
            }
        }

        private bool ConfirmDiscardUnsavedChanges()
        {
            if (Project == null) return true;
            try
            {
                if (!Project.HasUnsavedChanges()) return true;
            }
            catch
            {
                // If we can't determine state, fall through and prompt to be safe.
            }

            return MessageDialog.Confirm("Unsaved changes",
                "This project has unsaved changes. Discard them?",
                "Discard", "Keep editing", MessageDialogKind.Warning);
        }

        private void SaveProject()
        {
            Project?.Save();
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(IsDirty));
        }

        private void SaveProjectAs()
        {
            if (Project == null) return;
            var sfd = new SaveFileDialog
            {
                RestoreDirectory = true,
                Title = $"Save {Project.ProjectName}",
                FileName = $"{Project.ProjectName}.ssproj",
                Filter = "SirenSharp Project (*.ssproj)|*.ssproj"
            };

            if (sfd.ShowDialog() == true)
            {
                Project.SaveAs(sfd.FileName);
                // Re-point the project at its new file so dirty tracking compares against it.
                Project.ProjectPath = sfd.FileName;
                AddRecentProject(sfd.FileName);
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(IsDirty));
            }
        }

        private void NewAwc()
        {
            Project?.SoundSets.Add(new SoundSet("new_soundset"));
            RefreshValidationState();
        }

        private void NewSiren()
        {
            CurrentSoundSet?.AddSound(new Sound { Name = "new_siren" });
            RefreshValidationState();
        }

        private void ImportWav()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Wave File (*.wav)|*.wav",
                RestoreDirectory = true,
                Multiselect = true,
                Title = "Select WAV files"
            };

            if (ofd.ShowDialog() == true)
                ImportWavFiles(ofd.FileNames);
        }

        private void DeleteAwc()
        {
            if (CurrentSoundSet == null || Project == null) return;
            if (!MessageDialog.Confirm("Delete soundset",
                $"Delete soundset '{CurrentSoundSet.Name}' and all its sirens?", "Delete", "Cancel"))
                return;

            Project.SoundSets.Remove(CurrentSoundSet);
            CurrentSoundSet = null;
            RefreshValidationState();
        }

        private void DeleteSiren()
        {
            if (CurrentSoundSet == null || CurrentSiren == null) return;
            if (!MessageDialog.Confirm("Delete siren", $"Delete siren '{CurrentSiren.Name}'?", "Delete", "Cancel"))
                return;

            CurrentSoundSet.Sounds.Remove(CurrentSiren);
            CurrentSiren = null;
            OnPropertyChanged(nameof(SoundSetInfoText));
            RefreshValidationState();
        }

        private void BrowseSiren()
        {
            if (CurrentSiren == null) return;
            var ofd = new OpenFileDialog
            {
                Filter = "Wave File (*.wav)|*.wav",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == true && File.Exists(ofd.FileName))
                CurrentSirenPath = ofd.FileName;
        }

        private void GenerateResource()
        {
            if (Project == null) return;

            if (Project.SoundSets.Count > 7)
            {
                if (!MessageDialog.Confirm("Soundset limit",
                    "This project has more than 7 soundsets. FiveM may only support 7 concurrent audio banks.\n\nContinue anyway?",
                    "Continue", "Cancel", MessageDialogKind.Warning))
                    return;
            }

            var vm = serviceProvider.GetRequiredService<GenerateResourceViewModel>();
            vm.ResourceName = Project.ProjectName.ToLower().Replace(" ", "_");
            vm.DlcName = string.IsNullOrWhiteSpace(Project.DLCName) ? vm.DlcName : Project.DLCName;
            vm.FxVersion = appSettings.Settings.DefaultFxVersion;

            var dialog = new GenerateResourceWindow { DataContext = vm };
            if (dialog.ShowDialog() != true) return;

            Project.DLCName = vm.DlcName;
            appSettings.Settings.DefaultFxVersion = vm.FxVersion;
            appSettings.Save();

            var resourceDir = Path.Combine(vm.ResourcePath, vm.ResourceName);
            if (Directory.Exists(resourceDir))
            {
                if (!MessageDialog.Confirm("Resource exists",
                    $"A resource named '{vm.ResourceName}' already exists in that folder. Replace it?",
                    "Replace", "Cancel", MessageDialogKind.Warning))
                    return;
                Directory.Delete(resourceDir, true);
            }

            var options = new ResourceGenerationOptions
            {
                ResourceName = vm.ResourceName,
                DlcName = vm.DlcName,
                FolderPath = vm.ResourcePath,
                FxVersion = vm.FxVersion,
                GenerateInGameTester = vm.GenerateTester,
                SoundSets = Project.SoundSets.ToList()
            };

            // Run generation off the UI thread behind a modal progress dialog so the app
            // stays responsive. ShowDialog() pumps messages, so the progress ring animates
            // and the IProgress<string> callbacks marshal back to the UI thread.
            IsGenerating = true;
            StatusBarText = "Generating resource...";

            ResourceGenerationResult? result = null;
            var progressWindow = new ProgressWindow { Owner = Application.Current.MainWindow };
            var progress = new Progress<string>(progressWindow.SetStatus);

            progressWindow.Loaded += async (_, _) =>
            {
                try
                {
                    result = await Task.Run(() => exporter.Export(options, progress));
                }
                catch (Exception ex)
                {
                    result = new ResourceGenerationResult();
                    result.Diagnostics.AddError($"Unexpected error: {ex.Message}", DiagnosticCodes.Unexpected);
                }
                finally
                {
                    progressWindow.Close();
                }
            };
            progressWindow.ShowDialog();

            IsGenerating = false;

            if (result != null)
                ShowGenerationResult(result, vm.ResourceName);
        }

        private void ShowGenerationResult(ResourceGenerationResult result, string resourceName)
        {
            lastGenerationResult = result;
            LastGenerationSucceeded = result.Success;
            LastGenerationDetails.Clear();

            if (result.Success)
            {
                LastGeneratedResourcePath = result.ResourcePath;
                StatusBarText = $"Resource '{resourceName}' generated successfully.";
                LastGenerationTitle = "Resource generated";
                LastGenerationMessage = $"'{resourceName}' was built successfully and is ready to drop into your FiveM server.";

                foreach (var w in result.Diagnostics.Warnings)
                    LastGenerationDetails.Add($"Warning: {w}");
                foreach (var v in result.AwcVerifications)
                    LastGenerationDetails.Add(v.Summary);
                if (!string.IsNullOrWhiteSpace(result.TesterPath))
                    LastGenerationDetails.Add($"In-game tester: {result.TesterPath} (/sirentest)");
            }
            else
            {
                StatusBarText = "Generation failed.";
                LastGenerationTitle = "Generation failed";
                LastGenerationMessage = "SirenSharp could not build the resource. See the details below and fix the listed issues.";
                foreach (var err in result.Diagnostics.Errors)
                    LastGenerationDetails.Add(err.ToString());
            }

            OnPropertyChanged(nameof(LastGenerationSucceeded));
            OnPropertyChanged(nameof(LastGenerationTitle));
            OnPropertyChanged(nameof(LastGenerationMessage));

            new GenerationResultWindow { DataContext = this }.ShowDialog();
        }

        // Writes a diagnostics report (project state, preflight findings, and the last
        // build result if any) to a file the user picks - useful for bug reports.
        private void ExportDiagnostics()
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var sfd = new SaveFileDialog
            {
                Title = "Export diagnostics",
                FileName = $"sirensharp-diagnostics-{stamp}.txt",
                DefaultExt = ".txt",
                Filter = "Text report (*.txt)|*.txt|JSON report (*.json)|*.json",
                RestoreDirectory = true
            };

            if (sfd.ShowDialog() != true) return;

            try
            {
                var snapshot = diagnosticsExporter.Capture(Project, lastGenerationResult);
                diagnosticsExporter.Export(snapshot, sfd.FileName);
                StatusBarText = $"Diagnostics exported to {sfd.FileName}";
            }
            catch (Exception ex)
            {
                MessageDialog.Error("Export failed", $"Could not write the diagnostics report: {ex.Message}");
            }
        }

        private void PlaySound(Sound? sound)
        {
            if (sound == null) return;
            CurrentSiren = sound;
            PlaySiren();
        }

        private void FixSound(Sound? sound)
        {
            if (sound == null) return;
            CurrentSiren = sound;
            FixSirenAudio();
        }

        private void DeleteSound(Sound? sound)
        {
            if (sound == null || CurrentSoundSet == null) return;
            if (!MessageDialog.Confirm("Delete siren", $"Delete siren '{sound.Name}'?", "Delete", "Cancel"))
                return;

            CurrentSoundSet.Sounds.Remove(sound);
            if (ReferenceEquals(CurrentSiren, sound)) CurrentSiren = null;
            OnPropertyChanged(nameof(SoundSetInfoText));
            RefreshValidationState();
        }

        private void OpenOutputFolder()
        {
            if (string.IsNullOrWhiteSpace(LastGeneratedResourcePath) || !Directory.Exists(LastGeneratedResourcePath)) return;
            Process.Start(new ProcessStartInfo { FileName = LastGeneratedResourcePath, UseShellExecute = true });
        }

        private void LoadRecentProjects()
        {
            RecentProjects.Clear();
            foreach (var path in appSettings.Settings.RecentProjects.Where(File.Exists).Take(8))
                RecentProjects.Add(new RecentProjectInfo(Path.GetFileNameWithoutExtension(path), path));
        }

        private void PlaySiren()
        {
            if (CurrentSiren == null || !File.Exists(CurrentSiren.AudioPath)) return;
            try
            {
                audioPreview.PlayWav(CurrentSiren.AudioPath);
                StatusBarText = $"Playing: {CurrentSiren.Name}";
                (StopPreviewCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageDialog.Error("Playback error", ex.Message);
            }
        }

        private void StopPreview()
        {
            audioPreview.Stop();
            StatusBarText = "Ready";
        }

        // Where converted/temporary WAVs are written. Prefer a folder next to the
        // project file (so they travel with the project and are easy to find); fall
        // back to LocalAppData when there's no saved project path yet.
        private string GetConvertedFilesDir()
        {
            string dir;
            if (Project != null && !string.IsNullOrWhiteSpace(Project.ProjectPath))
            {
                var projectDir = Path.GetDirectoryName(Project.ProjectPath)!;
                dir = Path.Combine(projectDir, $"{Project.ProjectName}_files");
            }
            else
            {
                dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SirenSharp", "Fixed");
            }

            Directory.CreateDirectory(dir);
            return dir;
        }

        private void FixSirenAudio()
        {
            if (CurrentSiren == null || !File.Exists(CurrentSiren.AudioPath)) return;

            var fixedDir = GetConvertedFilesDir();

            var baseName = Path.GetFileNameWithoutExtension(CurrentSiren.FileName);
            if (string.IsNullOrWhiteSpace(baseName)) baseName = CurrentSiren.Name;
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            var output = Path.Combine(fixedDir, $"{baseName}_{unique}.wav");

            var result = wavSanitizer.Sanitize(CurrentSiren.AudioPath, output);
            if (!result.Success)
            {
                MessageDialog.Error("Fix failed", result.Error ?? "The WAV could not be converted.");
                return;
            }

            CurrentSirenPath = output;
            StatusBarText = $"Fixed audio: {string.Join(", ", result.Changes)}";
        }

        private void OpenInRpfExplorer()
        {
            if (string.IsNullOrWhiteSpace(LastGeneratedResourcePath)) return;

            if (LaunchRpfExplorer()) return;

            var path = LocateTool("CodeWalker", "CodeWalker.exe");
            if (path == null) return;

            externalTools.CodeWalkerPath = path;
            appSettings.Settings.CodeWalkerPath = path;
            appSettings.Save();

            if (!LaunchRpfExplorer())
                MessageDialog.Error("CodeWalker", "Could not start CodeWalker from that location.");
        }

        // CodeWalker can't be told to open a folder from the command line, so we
        // launch its RPF Explorer mode, copy the resource path to the clipboard,
        // and tell the user how to load it (File -> Open Folder).
        private bool LaunchRpfExplorer()
        {
            if (!externalTools.TryOpenRpfExplorer()) return false;

            try { System.Windows.Clipboard.SetText(LastGeneratedResourcePath!); }
            catch { /* clipboard can fail if another app holds it; not critical */ }

            MessageDialog.Info("CodeWalker RPF Explorer",
                "CodeWalker has no way to open a folder directly from another app, so its RPF Explorer has been launched.\n\n" +
                "Your resource path has been copied to the clipboard. In RPF Explorer, click File \u2192 Open Folder and paste it in to browse and play your .awc files.");
            return true;
        }

        // Prompts the user to locate a tool's executable. Returns the chosen path or null.
        private static string? LocateTool(string toolName, string exeName)
        {
            if (!MessageDialog.Confirm($"Locate {toolName}",
                $"SirenSharp couldn't find {toolName} automatically. Would you like to point it to {exeName}?",
                $"Locate {exeName}", "Cancel", MessageDialogKind.Info))
                return null;

            var ofd = new OpenFileDialog
            {
                Title = $"Locate {exeName}",
                Filter = $"{toolName} ({exeName})|{exeName}|Executable (*.exe)|*.exe",
                RestoreDirectory = true
            };

            return ofd.ShowDialog() == true && File.Exists(ofd.FileName) ? ofd.FileName : null;
        }

        private void ValidateField(string propertyName, System.Windows.Controls.ValidationResult validationResult)
        {
            errorsViewModel.ClearErrors();
            if (!validationResult.IsValid)
                errorsViewModel.AddError(validationResult.ErrorContent?.ToString() ?? "Invalid value");
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            RefreshValidationState();
        }

        private void RefreshValidationState()
        {
            PreflightReport = Project != null ? preflight.Inspect(Project) : new DiagnosticReport();
            StatusBarText = PreflightReport.Errors.FirstOrDefault()?.ToString() ?? "Ready";
            OnPropertyChanged(nameof(PreflightReport));
            OnPropertyChanged(nameof(PreflightSummary));
            OnPropertyChanged(nameof(CanGenerateResource));
            OnPropertyChanged(nameof(HasValidationError));
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(SoundSetInfoText));
            (GenerateResourceCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (SaveProjectCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (SaveProjectAsCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (NewAwcCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (NewSirenCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (ImportWavCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (DeleteAwcCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (DeleteSirenCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (BrowseSirenCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (PlaySirenCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (FixSirenAudioCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void AddRecentProject(string path)
        {
            appSettings.Settings.RecentProjects.Remove(path);
            appSettings.Settings.RecentProjects.Insert(0, path);
            appSettings.Settings.RecentProjects = appSettings.Settings.RecentProjects.Take(10).ToList();
            appSettings.Save();
            LoadRecentProjects();
        }

        public IEnumerable GetErrors(string? propertyName) => errorsViewModel.GetErrors(propertyName);
    }

    public record RecentProjectInfo(string Name, string Path);
}
