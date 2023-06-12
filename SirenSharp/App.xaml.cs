using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SirenSharp.Services;
using SirenSharp.ViewModels;
using SirenSharp.Views;

namespace SirenSharp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services
                .AddTransient<IAwcGenerator, AwcGenerator>()
                .AddTransient<IDataGenerator, DataGenerator>()
                .AddTransient<IResourceGenerator, ResourceGenerator>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>(services => new MainWindow
                {
                    DataContext = services.GetRequiredService<MainViewModel>()
                });
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
