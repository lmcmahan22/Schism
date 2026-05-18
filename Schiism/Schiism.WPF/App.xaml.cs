// <copyright file="App.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism
{
    using System.ComponentModel;
    using System.Configuration;
    using System.Data;
    using System.Windows;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.WPF;
    using Schiism.WPF.IPC;
    using Schiism.WPF.Models.Implementations;
    using Schiism.WPF.Services;
    using Schiism.WPF.Views;

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
            // Local Singleton
            containerRegistry.RegisterSingleton<ThemeController>();

            // Core Singletons
            containerRegistry.RegisterSingleton<WPFCommandReceiver>();
            containerRegistry.RegisterSingleton<WPFCommandSender>();
            containerRegistry.RegisterSingleton<WPFStreamSubscriber<ConnSettings>>();
            containerRegistry.RegisterSingleton<WPFStreamSubscriber<ModbusData>>();

            containerRegistry.RegisterSingleton<IStreamDataState<ModbusData>, StreamDataState<ModbusData>>();
            containerRegistry.RegisterSingleton<IStreamDataState<ConnectionDiagnostics>, StreamDataState<ConnectionDiagnostics>>();

            containerRegistry.RegisterSingleton<ICommandState, ModbusConfigState>();
        }

        // Configures the module catalog, which is responsible for managing the modules in the application. This method is called during application initialization.
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<MainModule>();
        }
    }
}