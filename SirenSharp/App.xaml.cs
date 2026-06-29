using Microsoft.Extensions.DependencyInjection;
using SirenSharp.Services;
using SirenSharp.Services.Backends;
using SirenSharp.Services.Exporters;
using SirenSharp.Services.Preflight;
using SirenSharp.ViewModels;
using SirenSharp.Views;
using System.Windows;
using Velopack;
using Wpf.Ui.Appearance;

namespace SirenSharp
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider = null!;

        public App()
        {
            // Must run before any other app logic. On Velopack install/update hook
            // invocations this handles the hook and exits; for normal launches and
            // portable builds it returns immediately.
            VelopackApp.Build().Run();

            var services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<IServiceProvider>(sp => sp)
                .AddSingleton<AppSettingsService>()
                .AddSingleton<UpdateService>()
                .AddSingleton<ExternalToolLauncher>()
                .AddSingleton<AudioPreviewService>()
                .AddTransient<WavFormatAnalyzer>()
                .AddTransient<WavSanitizer>()
                .AddTransient<AwcGenerator>()
                .AddTransient<DataGenerator>()
                .AddTransient<AwcVerifier>()
                .AddTransient<IAwcBuildBackend, CodeWalkerAwcBuildBackend>()
                .AddTransient<AudioPackBuilder>()
                .AddTransient<IResourceExporter, GenericFiveMExporter>()
                .AddTransient<IPreflightCheck, ProjectStructureCheck>()
                .AddTransient<IPreflightCheck, SoundSetCheck>()
                .AddTransient<IPreflightCheck, SirenAudioCheck>()
                .AddSingleton<PreflightService>()
                .AddTransient<DiagnosticsExporter>()
                .AddTransient<NewProjectViewModel>()
                .AddTransient<GenerateResourceViewModel>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>(sp => new MainWindow
                {
                    DataContext = sp.GetRequiredService<MainViewModel>()
                });
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var settings = serviceProvider.GetRequiredService<AppSettingsService>();
            ApplicationThemeManager.Apply(settings.Settings.UseDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light);

            var externalTools = serviceProvider.GetRequiredService<ExternalToolLauncher>();
            externalTools.CodeWalkerPath = settings.Settings.CodeWalkerPath;
            externalTools.TryDetectTools();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider.GetService<AudioPreviewService>()?.Dispose();

            var settings = serviceProvider.GetService<AppSettingsService>();
            var externalTools = serviceProvider.GetService<ExternalToolLauncher>();
            if (settings != null && externalTools != null)
            {
                settings.Settings.CodeWalkerPath = externalTools.CodeWalkerPath;
                settings.Save();
            }

            serviceProvider.Dispose();
            base.OnExit(e);
        }
    }
}
