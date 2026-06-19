using Microsoft.Extensions.DependencyInjection;
using SirenSharp.Services;
using SirenSharp.ViewModels;
using SirenSharp.Views;
using System.Windows;
using Wpf.Ui.Appearance;

namespace SirenSharp
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider = null!;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<IServiceProvider>(sp => sp)
                .AddSingleton<AppSettingsService>()
                .AddSingleton<ExternalToolLauncher>()
                .AddSingleton<AudioPreviewService>()
                .AddTransient<WavFormatAnalyzer>()
                .AddTransient<WavSanitizer>()
                .AddTransient<AwcGenerator>()
                .AddTransient<DataGenerator>()
                .AddTransient<AwcVerifier>()
                .AddTransient<ResourceGenerator>()
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
