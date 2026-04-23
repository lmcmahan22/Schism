// <copyright file="App.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schism
{
    using System.ComponentModel;
    using System.Configuration;
    using System.Data;
    using System.Windows;
    using Schism.Services;
    using Schism.ViewModels;
    using Schism.Views;

    public partial class App
    {
        // Creates the main application window (shell) and returns it to be displayed.
        protected override Window CreateShell()
        {
            return this.Container.Resolve<MainWindow>();
        }

        // Registers types with the dependency injection container. This method is called during application initialization.
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
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