// <copyright file="App.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.WPF;
    using Schiism.WPF.Implementations.Modbus;
    using Schiism.WPF.IPC;
    using Schiism.WPF.IPC.Workers;
    using Schiism.WPF.Models.Implementations.States;
    using Schiism.WPF.Services;
    using Schiism.WPF.Views;
    using System.Windows;

    public partial class App
    {
        private readonly List<IHostedService> hostedServices = [];
        private readonly CancellationTokenSource appCancellation = new();

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

            // State Singletons (for content tracking)
            containerRegistry.RegisterSingleton<IStreamDataState<ModbusData>, StreamDataState<ModbusData>>();
            containerRegistry.RegisterSingleton<IStreamDataState<ConnectionDiagnostics>, StreamDataState<ConnectionDiagnostics>>();
            containerRegistry.RegisterSingleton<IWPFConfigState, WPFConfigState>();

            // IPC Singletons (Subscribers, Command Receiver, and Command Sender)
            containerRegistry.RegisterSingleton<ICommandReceiver>(
                cr => new WPFCommandReceiver(
                    PipeConstants.SettingsCommandName));
            containerRegistry.RegisterSingleton<ICommandSender>(
                cs => new WPFCommandSender(
                    PipeConstants.SettingsCommandName));
            containerRegistry.RegisterSingleton<IStreamSubscriber<ConnectionDiagnostics>>(
                ssc => new WPFStreamSubscriber<ConnectionDiagnostics>(
                    PipeConstants.ConnDiagStreamName,
                    ssc.Resolve<IStreamDataState<ConnectionDiagnostics>>()));
            containerRegistry.RegisterSingleton<IStreamSubscriber<ModbusData>>(
                ssm => new WPFStreamSubscriber<ModbusData>(
                    PipeConstants.ModbusDataStreamName,
                    ssm.Resolve<IStreamDataState<ModbusData>>()));

            // Workers (to run the subscription and command loops/calls)
            // NOTE: These need to resolve these as prism instances vis container.resolve
            containerRegistry.Register<CommandsWorker>();
            containerRegistry.Register<ConnDiagSubscriberWorker>();
            containerRegistry.Register<ModbusSubscriberWorker>();
        }

        // Configures the module catalog, which is responsible for managing the modules in the application. This method is called during application initialization.
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<MainModule>();
        }

        // Starts our three workers
        protected override async void OnInitialized()
        {
            base.OnInitialized();

            // This resolves the workers 
            hostedServices.Add(Container.Resolve<CommandsWorker>());
            hostedServices.Add(Container.Resolve<ConnDiagSubscriberWorker>());
            hostedServices.Add(Container.Resolve<ModbusSubscriberWorker>());

            foreach (var service in hostedServices)
            {
                await service.StartAsync(CancellationToken.None);
            }
        }

        // Stop the three workers on shutdown
        protected override async void OnExit(ExitEventArgs e)
        {
            foreach (var service in hostedServices)
            {
                await service.StopAsync(appCancellation.Token);
            }

            appCancellation.Cancel();
            base.OnExit(e);
        }
    }
}