// <copyright file="App.xaml.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Prism.Ioc;
    using Schiism.Core;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.Commands;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.Serialization;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.IPC.Streams;
    using Schiism.Core.Logging;
    using Schiism.WPF;
    using Schiism.WPF.Tabs;
    using Schiism.WPF.IPC;
    using Schiism.WPF.Views;
    using System.Runtime.Intrinsics.X86;
    using System.Windows;
    using Schiism.WPF.Models;

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
                    new FileLoggerProvider(
                        @"C:\Users\lmcmahan\OneDrive - Precision Valve and Automation\Desktop\SchiismLogs", "WPF"));
            });

            containerRegistry.RegisterInstance<ILoggerFactory>(loggerFactory);

            // State Singletons (for content tracking)
            // containerRegistry.RegisterSingleton<StreamDataState<ModbusData>, StreamStore<ModbusData>>();
            // containerRegistry.RegisterSingleton<StreamDataState<ConnectionDiagnostics>, StreamStore<ConnectionDiagnostics>>();
            containerRegistry.RegisterSingleton<ConfigState>();
            containerRegistry.RegisterSingleton<ModbusWriteState>();
            containerRegistry.RegisterSingleton<InitStatus>();
            containerRegistry.RegisterSingleton<INamedPipeFactory, BasePipeFactory>();
            containerRegistry.RegisterSingleton<PipeSerializer>();
            containerRegistry.RegisterSingleton<StreamStore<ModbusDataCollectionDTO>>();
            containerRegistry.RegisterSingleton<StreamStore<ConnDiagDTO>>();

            // Theme Controller Singleton
            containerRegistry.RegisterSingleton<ThemesControl>();

            // IPC Singletons (Subscribers, Command Receiver, and Command Sender)
            containerRegistry.RegisterSingleton<CommandReceiver<SettingsConfigDTO>>(
                cr => new CommandReceiver<SettingsConfigDTO>(
                    NamingConstants.InitSettingsCommandName,
                    cr.Resolve<INamedPipeFactory>(),
                    cr.Resolve<PipeSerializer>(),
                    cr.Resolve<ILoggerFactory>().CreateLogger<CommandReceiver<SettingsConfigDTO>>()));

            containerRegistry.RegisterSingleton<CommandSender<SettingsConfigDTO>>(
                cs => new CommandSender<SettingsConfigDTO>(
                    NamingConstants.SettingsCommandName,
                    cs.Resolve<INamedPipeFactory>(),
                    cs.Resolve<PipeSerializer>(),
                    cs.Resolve<ILoggerFactory>().CreateLogger<CommandSender<SettingsConfigDTO>>()));

            containerRegistry.RegisterSingleton<CommandSender<ModbusWriteDTO>>(
                cs => new CommandSender<ModbusWriteDTO>(
                    NamingConstants.ModbusWriteCommandName,
                    cs.Resolve<INamedPipeFactory>(),
                    cs.Resolve<PipeSerializer>(),
                    cs.Resolve<ILoggerFactory>().CreateLogger<CommandSender<ModbusWriteDTO>>()));

            containerRegistry.RegisterSingleton<StreamSubscriber<ConnDiagDTO>>(
                ssc => new StreamSubscriber<ConnDiagDTO>(
                    ssc.Resolve<PipeSerializer>(),
                    ssc.Resolve<ILoggerFactory>().CreateLogger<StreamSubscriber<ConnDiagDTO>>()));

            containerRegistry.RegisterSingleton<StreamSubscriber<ModbusDataCollectionDTO>>(
                ssm => new StreamSubscriber<ModbusDataCollectionDTO>(
                    ssm.Resolve<PipeSerializer>(),
                    ssm.Resolve<ILoggerFactory>().CreateLogger<StreamSubscriber<ModbusDataCollectionDTO>>()));

            // Workers (to run the subscription and command loops/calls)
            containerRegistry.Register<CommandsWorker>(
            cw => new CommandsWorker(
                cw.Resolve<CommandReceiver<SettingsConfigDTO>>(),
                cw.Resolve<INamedPipeFactory>(),
                cw.Resolve<CommandSender<SettingsConfigDTO>>(),
                cw.Resolve<CommandSender<ModbusWriteDTO>>(),
                cw.Resolve<ConfigState>(),
                cw.Resolve<ModbusWriteState>(),
                cw.Resolve<InitStatus>(),
                cw.Resolve<ILoggerFactory>().CreateLogger<CommandsWorker>()));
            containerRegistry.Register<StreamSubscriberWorker<ModbusDataCollectionDTO>>(
            swm => new StreamSubscriberWorker<ModbusDataCollectionDTO>(
                NamingConstants.ModbusDataStreamName,
                swm.Resolve<INamedPipeFactory>(),
                swm.Resolve<StreamSubscriber<ModbusDataCollectionDTO>>(),
                swm.Resolve<StreamStore<ModbusDataCollectionDTO>>(),
                swm.Resolve<InitStatus>(),
                swm.Resolve<ILoggerFactory>().CreateLogger<StreamSubscriberWorker<ModbusDataCollectionDTO>>()));
            containerRegistry.Register<StreamSubscriberWorker<ConnDiagDTO>>(
                swc => new StreamSubscriberWorker<ConnDiagDTO>(
                NamingConstants.ConnDiagStreamName,
                swc.Resolve<INamedPipeFactory>(),
                swc.Resolve<StreamSubscriber<ConnDiagDTO>>(),
                swc.Resolve<StreamStore<ConnDiagDTO>>(),
                swc.Resolve<InitStatus>(),
                swc.Resolve<ILoggerFactory>().CreateLogger<StreamSubscriberWorker<ConnDiagDTO>>()));
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
            hostedServices.Add(Container.Resolve<StreamSubscriberWorker<ModbusDataCollectionDTO>>());
            hostedServices.Add(Container.Resolve<StreamSubscriberWorker<ConnDiagDTO>>());

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