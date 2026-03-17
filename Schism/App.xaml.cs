using Schism.Models;
using Schism.ViewModels;
using Schism.Views;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Schism
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {

        // Creates the main application window (shell) and returns it to be displayed.
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        // Registers types with the dependency injection container. This method is called during application initialization.
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<SaveAndLoadService>();
            containerRegistry.RegisterSingleton<MODBUSService>();
            containerRegistry.RegisterSingleton<ThemeService>();
        }

        // Configures the module catalog, which is responsible for managing the modules in the application. This method is called during application initialization.
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<MainModule>();
        }
    }
}