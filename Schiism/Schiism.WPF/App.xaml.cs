// <copyright file="App.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Prism.Ioc;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.WPF;
    using Schiism.WPF.FileLogging;
    using Schiism.WPF.Implementations.Modbus;
    using Schiism.WPF.IPC;
    using Schiism.WPF.IPC.Workers;
    using Schiism.WPF.Models.Implementations.States;
    using Schiism.WPF.Services;
    using Schiism.WPF.Views;
    using System.Runtime.Intrinsics.X86;
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

            // Logger factory to be used via DI (a bit verbose in order to get it to work how we want...)
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();

                builder.AddProvider(
                    new WPFFileLoggerProvider(
                        @"C:\Users\lmcmahan\OneDrive - Precision Valve and Automation\Desktop\SchiismLogs"));
            });

            containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);

            // Local Singleton
            containerRegistry.RegisterSingleton<ThemeController>();

            // State Singletons (for content tracking)
            containerRegistry.RegisterSingleton<IStreamDataState<ModbusData>, StreamDataState<ModbusData>>();
            containerRegistry.RegisterSingleton<IStreamDataState<ConnectionDiagnostics>, StreamDataState<ConnectionDiagnostics>>();
            containerRegistry.RegisterSingleton<IWPFConfigState, WPFConfigState>();
            containerRegistry.RegisterSingleton<IInitializedState, InitializedState>();

            // IPC Singletons (Subscribers, Command Receiver, and Command Sender)
            containerRegistry.RegisterSingleton<ICommandReceiver>(
                cr => new WPFCommandReceiver(
                    PipeConstants.SettingsCommandName));
            containerRegistry.RegisterSingleton<ICommandSender>(
                cs => new WPFCommandSender(
                    PipeConstants.SettingsCommandName));
            containerRegistry.RegisterSingleton<IStreamSubscriber<ConnectionDiagnostics>>(
                ssc => new WPFStreamSubscriber<ConnectionDiagnostics>(
                    ssc.Resolve<ILoggerFactory>()));
            containerRegistry.RegisterSingleton<IStreamSubscriber<ModbusData>>(
                ssm => new WPFStreamSubscriber<ModbusData>(
                    ssm.Resolve<ILoggerFactory>()));

            // Workers (to run the subscription and command loops/calls)
            containerRegistry.Register<WPFCommandsWorker>(
            cw => new WPFCommandsWorker(
                cw.Resolve<ICommandReceiver>(),
                cw.Resolve<ICommandSender>(),
                cw.Resolve<IWPFConfigState>(),
                cw.Resolve<IInitializedState>(),
                cw.Resolve<ILoggerFactory>()));
            containerRegistry.Register<WPFSubscriberWorker<ModbusData>>(
            swm => new WPFSubscriberWorker<ModbusData>(
                PipeConstants.ModbusDataStreamName,
                swm.Resolve<IStreamSubscriber<ModbusData>>(),
                swm.Resolve<IStreamDataState<ModbusData>>(),
                swm.Resolve<ILoggerFactory>()));
            containerRegistry.Register<WPFSubscriberWorker<ConnectionDiagnostics>>(
                swc => new WPFSubscriberWorker<ConnectionDiagnostics>(
                PipeConstants.ConnDiagStreamName,
                swc.Resolve<IStreamSubscriber<ConnectionDiagnostics>>(),
                swc.Resolve<IStreamDataState<ConnectionDiagnostics>>(),
                swc.Resolve<ILoggerFactory>()));
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
            hostedServices.Add(Container.Resolve<WPFCommandsWorker>());
            hostedServices.Add(Container.Resolve<WPFSubscriberWorker<ModbusData>>());
            hostedServices.Add(Container.Resolve<WPFSubscriberWorker<ConnectionDiagnostics>>());

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