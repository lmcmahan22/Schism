// <copyright file="Program.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service
{
    using System.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting.WindowsServices;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Logging;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.DTOs.IPC_Records.Commands;
    using Schiism.Core.Models.DTOs.IPC.Streams;
    using Schiism.IPC;
    using Schiism.IPC.Models.Pipes.Commands;
    using Schiism.IPC.Models.Pipes.Streams;
    using Schiism.Service.Models.FileLogging;
    using Schiism.Service.Models.Implementations;
    using Schiism.Service.Models.Implementations.IPC;
    using Schiism.Service.Models.Implementations.Modbus;
    using Schiism.Service.Models.Implementations.Publishers;
    using Schiism.Service.Models.Workers;
    using Schiism.Service.Models.Workers.Commands;
    using Schiism.Service.Models.Workers.Streams;

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

            bool isService = WindowsServiceHelpers.IsWindowsService();
            if (isService)
            {
                builder.Services.AddWindowsService();
                builder.Logging.ClearProviders();
                builder.Logging.AddProvider(new FileLoggerProvider($"C:\\Users\\lmcmahan\\OneDrive - Precision Valve and Automation\\Desktop\\SchiismLogs"));
                builder.Services.AddSingleton<IDataPublisher, IPCDataPublisher>();

                // Control via checkbox in UI when it's ready!
                ConfigureStartup(true);
            }
            else
            {
                builder.Logging.AddConsole();
                builder.Services.AddSingleton<IDataPublisher, ConsoleDataPublisher>();
            }

            // Define Core Services (Builder.Services is a dependency container)
            // Make this its own method (ChatGPT originally advised making this its own CLASS???)
            builder.Services.AddSingleton<IModbusEngine, ModbusEngine>();
            builder.Services.AddSingleton<IModbusClient, ModbusClient>();
            builder.Services.AddSingleton<IModbusConfig, ModbusConfig>();
            builder.Services.AddSingleton<IEnginePublisher, EnginePublisher>();
            builder.Services.AddSingleton<IModbusInterpreter, ModbusInterpreter>();

            // Stream Publishers
            builder.Services.AddSingleton<IStreamPublisher<ModbusData>>(sp =>
                new NamedPipeStreamPublisher<ModbusData>(PipeConstants.ModbusDataStream));

            builder.Services.AddSingleton<IStreamPublisher<ConnectionDiagnostics>>(sp =>
                new NamedPipeStreamPublisher<ConnectionDiagnostics>(PipeConstants.ConnDiagStream));

            // Stream Queues
            builder.Services.AddSingleton<IStreamQueue<ModbusData>, ModbusStreamQueue>();
            builder.Services.AddSingleton<IStreamQueue<ConnectionDiagnostics>, ConnDiagStreamQueue>();

            // Commmand Server
            builder.Services.AddSingleton<ICommandServer<SettingsConfig>, NamedPipeCommandServer<SettingsConfig>>(sp =>
                new NamedPipeCommandServer<SettingsConfig>(PipeConstants.SettingsCommand));

            // Frontend
            // builder.Services.AddSingleton<ICommandClient<ModbusConfigCommand>>(sp =>
            //    new NamedPipeCommandClient<ModbusConfigCommand>(PipeConstants.ModbusSettingsCommand));

            // builder.Services.AddSingleton<ICommandClient<ConnectionConfigCommand>>(sp =>
            //    new NamedPipeCommandClient<ConnectionConfigCommand>(PipeConstants.ConnSettingsCommand));

            // builder.Services.AddSingleton<IStreamSubscriber<ModbusData>>(sp =>
            //    new NamedPipeStreamSubscriber<ModbusData>(PipeConstants.ModbusDataStream));

            // builder.Services.AddSingleton<IStreamSubscriber<ConnectionDiagnostics>>(sp =>
            //    new NamedPipeStreamSubscriber<ConnectionDiagnostics>(PipeConstants.ConnDiagStream));

            // Add an instance of the Worker classes as the hosted services (1 engine, 2 stream workers, 1 stream queue worker, 1 command worker)
            builder.Services.AddHostedService<ModbusEngineWorker>();
            builder.Services.AddHostedService<ModbusStreamWorker>();
            builder.Services.AddHostedService<ConnDiagStreamWorker>();
            builder.Services.AddHostedService<QueueMonitorWorker>();
            builder.Services.AddHostedService<SettingsCommandWorker>();

            // In WPF...
            // await commandClient.SendAsync(new ModbusConfigCommand(...));

            // Build and run!
            var host = builder.Build();
            host.Run();
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
                ConfigureRestart(true);
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