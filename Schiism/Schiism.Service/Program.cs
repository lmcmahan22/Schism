// <copyright file="Program.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Schiism.Core;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Abstractions.RuntimeControl;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.Service.FileLogging;
    using Schiism.Service.Implementations;
    using Schiism.Service.Implementations.IPC;
    using Schiism.Service.Implementations.Modbus;
    using Schiism.Service.Implementations.RuntimeControl;
    using Schiism.Service.Workers;
    using System.Diagnostics;

    /// <summary>
    /// Main Service program that executes the Worker Service, which runs your engine.
    /// Includes:
    ///    - Command line argument handling (currently just "-install")
    ///    - A HostBuilder for DI, Logging, and Config
    /// Notes:
    ///    - Logs can be sent to event viewer if desired! If so, add an EventLog instead of the FileProvider.
    ///    - Desktop batch script can start, publish, and install the app. "sc.exe start PVAModbusClient" only starts it.
    ///    - App is executed in console as ".\Schiism.Service.exe".
    ///    - For triggering an intentional crash for testing, enter the following in terminal from any file location: "taskkill /F /IM Schiism.Service.exe" (no quotes).
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            // Command line argument handling
            if (args.Length > 0)
            {
                ArgsHandling(args);
                return; // Prevent app startup
            }

            // Application run/start
            RunHost(args);
        }

        private static void RunHost(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddWindowsService();
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new FileLoggerProvider($"C:\\Users\\lmcmahan\\OneDrive - Precision Valve and Automation\\Desktop\\SchiismLogs"));

            // Control via checkboxes in UI when it's ready!
            var settingsStore = new ServiceSettingsStore();
            var settings = settingsStore.Load();

            ConfigureStartup(settings.AutoStart);
            ConfigureRestart(settings.AutoRestart);

            // Define Core Services (Builder.Services is a dependency container)
            // Make this its own method (ChatGPT originally advised making this its own CLASS???)
            builder.Services.AddSingleton<IConfigState, ConfigState>();
            builder.Services.AddSingleton<IModbusClient, ModbusClient>();
            builder.Services.AddSingleton<IEngine, Engine>();
            builder.Services.AddSingleton<IModbusInterpreter, ModbusInterpreter>();
            builder.Services.AddSingleton<IModbusControl, ModbusControl>();
            builder.Services.AddSingleton<IServiceSettingsStore, ServiceSettingsStore>();

            // ConnectionState
            builder.Services.AddSingleton<IInitializedState, FrontendInitializedState>();

            // Stream Publishers
            builder.Services.AddSingleton<IStreamPublisher<ModbusData>, ServiceStreamPublisher<ModbusData>>(
                sp => new ServiceStreamPublisher<ModbusData>(
                sp.GetRequiredService<ILogger<ServiceStreamPublisher<ModbusData>>>()));
            builder.Services.AddSingleton<IStreamPublisher<ConnectionDiagnostics>, ServiceStreamPublisher<ConnectionDiagnostics>>(
                sp => new ServiceStreamPublisher<ConnectionDiagnostics>(
                sp.GetRequiredService<ILogger<ServiceStreamPublisher<ConnectionDiagnostics>>>()));

            // Stream Queues
            builder.Services.AddSingleton<IStreamQueue<ModbusData>, ServiceStreamQueue<ModbusData>>();
            builder.Services.AddSingleton<IStreamQueue<ConnectionDiagnostics>, ServiceStreamQueue<ConnectionDiagnostics>>();

            // Commmand Server
            builder.Services.AddSingleton<ICommandReceiver, ServiceCommandReceiver>(
                sp => new ServiceCommandReceiver(
                NamingConstants.SettingsCommandName,
                sp.GetRequiredService<ILogger<ServiceCommandReceiver>>()));

            // Command Client (for first config population)
            builder.Services.AddSingleton<ICommandSender, ServiceCommandSender>(
                sp => new ServiceCommandSender(
                NamingConstants.InitSettingsCommandName,
                sp.GetRequiredService<ILogger<ServiceCommandSender>>()));

            // Add an instance of the Worker classes as the hosted services (1 engine, 2 stream workers, 1 stream queue worker, 1 command worker)
            builder.Services.AddHostedService<ServiceEngineWorker>(
                ew => new ServiceEngineWorker(
                    ew.GetRequiredService<IEngine>(),
                    ew.GetRequiredService<IConfigState>(),
                    ew.GetRequiredService<IModbusControl>(),
                    ew.GetRequiredService<ILogger<ServiceEngineWorker>>()));
            builder.Services.AddHostedService<ServiceStreamPublisherWorker<ModbusData>>(
                spm => new ServiceStreamPublisherWorker<ModbusData>(
                    NamingConstants.ModbusDataStreamName,
                    spm.GetRequiredService<IConfigState>(),
                    spm.GetRequiredService<IInitializedState>(),
                    spm.GetRequiredService<IStreamQueue<ModbusData>>(),
                    spm.GetRequiredService<IStreamPublisher<ModbusData>>(),
                    spm.GetRequiredService<ILogger<ServiceStreamPublisherWorker<ModbusData>>>()));
            builder.Services.AddHostedService<ServiceStreamPublisherWorker<ConnectionDiagnostics>>(
                spc => new ServiceStreamPublisherWorker<ConnectionDiagnostics>(
                    NamingConstants.ConnDiagStreamName,
                    spc.GetRequiredService<IConfigState>(),
                    spc.GetRequiredService<IInitializedState>(),
                    spc.GetRequiredService<IStreamQueue<ConnectionDiagnostics>>(),
                    spc.GetRequiredService<IStreamPublisher<ConnectionDiagnostics>>(),
                    spc.GetRequiredService<ILogger<ServiceStreamPublisherWorker<ConnectionDiagnostics>>>()));
            builder.Services.AddHostedService<ServiceCommandsWorker>(
                cw => new ServiceCommandsWorker(
                    cw.GetRequiredService<ICommandReceiver>(),
                    cw.GetRequiredService<ICommandSender>(),
                    cw.GetRequiredService<IConfigState>(),
                    cw.GetRequiredService<IModbusControl>(),
                    cw.GetRequiredService<IInitializedState>(),
                    cw.GetRequiredService<IServiceSettingsStore>(),
                    cw.GetRequiredService<ILogger<ServiceCommandsWorker>>()));

            // Not used right now, it's just a logger that doesn't actually log correctly atm
            // builder.Services.AddHostedService<QueueMonitorWorker>();

            // In WPF...
            // await commandClient.SendAsync(new ModbusConfigCommand(...));

            // Build and run!
            IHost host = builder.Build();

            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"Host startup failure {ex}");
                throw;
            }
        }

        private static void ArgsHandling(string[] args)
        {
            if (args.Contains("-install"))
            {
                string? exePath = Process.GetCurrentProcess().MainModule!.FileName!;
                RunSc($"stop {NamingConstants.ServiceName}");
                RunSc($"delete {NamingConstants.ServiceName}");
                RunSc($"create {NamingConstants.ServiceName} binPath= \"{exePath}\" start= auto");
                RunSc($"description {NamingConstants.ServiceName} \"MODBUS TCP Client\"");
                return;
            }
        }

        private static void ConfigureStartup(bool enableAutoStart)
        {
            {
                // "delayed-auto" also works in place of "auto" here, but took about 90 seconds longer on my desktop PC to start up. Keeping this as "auto" until I see reason to change it.
                string startType = enableAutoStart ? "auto" : "demand";
                RunSc($"config {NamingConstants.ServiceName} start= {startType}");
            }
        }

        private static void ConfigureRestart(bool enableRestart)
        {
            {
                if (enableRestart)
                {
                    // Configure auto restart, if the app crashes
                    RunSc($"failureflag {NamingConstants.ServiceName} 1");
                    RunSc($"failure {NamingConstants.ServiceName} reset= 0 actions= restart/5000/restart/5000/restart/5000");
                }
                else
                {
                    // Disable all failure actions (no restart)
                    RunSc($"failureflag {NamingConstants.ServiceName} 0");
                    RunSc($"failure {NamingConstants.ServiceName} reset= 0 actions= \"\"");
                }
            }
        }

        private static void RunSc(string arguments)
        {
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    Verb = "runas", // requires admin
                    CreateNoWindow = true,
                    UseShellExecute = true,
                });
            }
        }
    }
}