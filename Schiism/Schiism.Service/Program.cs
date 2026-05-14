// <copyright file="Program.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using Schiism.Service.FileLogging;
    using Schiism.Service.Implementations;
    using Schiism.Service.Implementations.IPC;
    using Schiism.Service.Implementations.Modbus;
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
        private const string ServiceName = "PVAModbusClient";

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
            ConfigureStartup(true);
            ConfigureRestart(false);

            // Define Core Services (Builder.Services is a dependency container)
            // Make this its own method (ChatGPT originally advised making this its own CLASS???)
            builder.Services.AddSingleton<IModbusConfig, ModbusConfig>();
            builder.Services.AddSingleton<IModbusClient, ModbusClient>();
            builder.Services.AddSingleton<IEngine, Engine>();
            builder.Services.AddSingleton<IModbusInterpreter, ModbusInterpreter>();
            builder.Services.AddSingleton<IModbusControl, ModbusControl>();

            // ConnectionState
            builder.Services.AddSingleton<IFrontendInitState, FrontendInitState>();

            // Stream Publishers
            builder.Services.AddSingleton<IStreamPublisher<ModbusData>, StreamPublisher<ModbusData>>(sp => new StreamPublisher<ModbusData>
                (PipeConstants.ModbusDataStreamName, sp.GetRequiredService<IFrontendInitState>(), sp.GetRequiredService<ILogger<StreamPublisher<ModbusData>>>()));
            builder.Services.AddSingleton<IStreamPublisher<ConnectionDiagnostics>, StreamPublisher<ConnectionDiagnostics>>(sp => new StreamPublisher<ConnectionDiagnostics>
                (PipeConstants.ConnDiagStreamName, sp.GetRequiredService<IFrontendInitState>(), sp.GetRequiredService<ILogger<StreamPublisher<ConnectionDiagnostics>>>()));

            // Stream Queues
            builder.Services.AddSingleton<IStreamQueue<ModbusData>, StreamQueue<ModbusData>>();
            builder.Services.AddSingleton<IStreamQueue<ConnectionDiagnostics>, StreamQueue<ConnectionDiagnostics>>();

            // Commmand Server
            builder.Services.AddSingleton<ICommandReceiver<SettingsConfig>, ServiceCommandReceiver<SettingsConfig>>(sp =>
                new ServiceCommandReceiver<SettingsConfig>
                (PipeConstants.SettingsCommandName, sp.GetRequiredService<ILogger<ServiceCommandReceiver<SettingsConfig>>>()));

            // Command Client (for first config population)
            builder.Services.AddSingleton<ICommandSender<SettingsConfig>, ServiceCommandSender<SettingsConfig>>(sp =>
                new ServiceCommandSender<SettingsConfig>
                (PipeConstants.InitSettingsCommandName, sp.GetRequiredService<ILogger<ServiceCommandSender<SettingsConfig>>>()));

            // Frontend
            // builder.Services.AddSingleton<ICommandClient<ModbusConfigCommand>>(sp =>
            //    new NamedPipeCommandClient<ModbusConfigCommand>(PipeConstants.ModbusSettingsCommandName));

            // builder.Services.AddSingleton<ICommandClient<ConnectionConfigCommand>>(sp =>
            //    new NamedPipeCommandClient<ConnectionConfigCommand>(PipeConstants.ConnSettingsCommandName));

            // builder.Services.AddSingleton<IStreamSubscriber<ModbusData>>(sp =>
            //    new NamedPipeStreamSubscriber<ModbusData>(PipeConstants.ModbusDataStreamName));

            // builder.Services.AddSingleton<IStreamSubscriber<ConnectionDiagnostics>>(sp =>
            //    new NamedPipeStreamSubscriber<ConnectionDiagnostics>(PipeConstants.ConnDiagStreamName));

            // Add an instance of the Worker classes as the hosted services (1 engine, 2 stream workers, 1 stream queue worker, 1 command worker)
            builder.Services.AddHostedService<ModbusEngineWorker>();
            builder.Services.AddHostedService<StreamPublisherWorker<ModbusData>>();
            builder.Services.AddHostedService<StreamPublisherWorker<ConnectionDiagnostics>>();
            builder.Services.AddHostedService<CommandsWorker>();

            // Not used right now, it's just a logger that doesn't actually log correctly atm
            // builder.Services.AddHostedService<QueueMonitorWorker>();

            // In WPF...
            // await commandClient.SendAsync(new ModbusConfigCommand(...));

            // Build and run!
            var host = builder.Build();

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
                var exePath = Process.GetCurrentProcess().MainModule!.FileName!;
                RunSc($"stop {ServiceName}");
                RunSc($"delete {ServiceName}");
                RunSc($"create {ServiceName} binPath= \"{exePath}\" start= auto");
                RunSc($"description {ServiceName} \"MODBUS TCP Client\"");
                return;
            }
        }

        private static void ConfigureStartup(bool enableAutoStart)
        {
            {
                // "delayed-auto" also works in place of "auto" here, but took about 90 seconds longer on my desktop PC to start up. Keeping this as "auto" until I see reason to change it.
                string startType = enableAutoStart ? "auto" : "demand";
                RunSc($"config {ServiceName} start= {startType}");
            }
        }

        private static void ConfigureRestart(bool enableRestart)
        {
            {
                if (enableRestart)
                {
                    // Configure auto restart, if the app crashes
                    RunSc($"failureflag {ServiceName} 1");
                    RunSc($"failure {ServiceName} reset= 0 actions= restart/5000/restart/5000/restart/5000");
                }
                else
                {
                    // Disable all failure actions (no restart)
                    RunSc($"failureflag {ServiceName} 0");
                    RunSc($"failure {ServiceName} reset= 0 actions= \"\"");
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