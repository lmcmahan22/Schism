// <copyright file="Program.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Schiism.Core;
    using Schiism.Core.Configuration.FileControl;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.Commands;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.Serialization;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.IPC.Streams;
    using Schiism.Core.Logging;
    using Schiism.Core.Modbus;
    using Schiism.Service.HostedServices;
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
            builder.Logging.AddProvider(new FileLoggerProvider($"C:\\Users\\lmcmahan\\OneDrive - Precision Valve and Automation\\Desktop\\SchiismLogs", "Service"));

            // Control via checkboxes in UI when it's ready!
            var settingsStore = new ServiceSettingsStore();
            var settings = settingsStore.Load();

            ConfigureStartup(settings.AutoStart);
            ConfigureRestart(settings.AutoRestart);

            // Define Core Services (Builder.Services is a dependency container)
            // Make this its own method (ChatGPT originally advised making this its own CLASS???)
            // No constructor parameters needed here, DI automatically resolves these! :D
            builder.Services.AddSingleton<ConfigState>();
            builder.Services.AddSingleton<ModbusClient>();
            builder.Services.AddSingleton<Engine>();
            builder.Services.AddSingleton<ModbusInterpreter>();
            builder.Services.AddSingleton<PollControl>();
            builder.Services.AddSingleton<ServiceSettingsStore>();
            builder.Services.AddSingleton<INamedPipeFactory, AdminPipeFactory>();
            builder.Services.AddSingleton<PipeSerializer>();

            // ConnectionState
            builder.Services.AddSingleton<InitStatus>();

            // Stream Publishers
            builder.Services.AddSingleton<StreamPublisher<ModbusDataDTO>, StreamPublisher<ModbusDataDTO>>(
                sp => new StreamPublisher<ModbusDataDTO>(
                sp.GetRequiredService<PipeSerializer>(),
                sp.GetRequiredService<ILogger<StreamPublisher<ModbusDataDTO>>>()));
            builder.Services.AddSingleton<StreamPublisher<ConnDiagDTO>, StreamPublisher<ConnDiagDTO>>(
                sp => new StreamPublisher<ConnDiagDTO>(
                sp.GetRequiredService<PipeSerializer>(),
                sp.GetRequiredService<ILogger<StreamPublisher<ConnDiagDTO>>>()));

            // Stream Queues
            builder.Services.AddSingleton<StreamQueue<ModbusDataDTO>, StreamQueue<ModbusDataDTO>>();
            builder.Services.AddSingleton<StreamQueue<ConnDiagDTO>, StreamQueue<ConnDiagDTO>>();

            // Commmand Server
            builder.Services.AddSingleton<CommandReceiver>(
                sp => new CommandReceiver(
                NamingConstants.SettingsCommandName,
                sp.GetRequiredService<INamedPipeFactory>(),
                sp.GetRequiredService<PipeSerializer>(),
                sp.GetRequiredService<ILogger<CommandReceiver>>()));

            // Command Client (for first config population)
            builder.Services.AddSingleton<CommandSender>(
                sp => new CommandSender(
                NamingConstants.InitSettingsCommandName,
                sp.GetRequiredService<INamedPipeFactory>(),
                sp.GetRequiredService<PipeSerializer>(),
                sp.GetRequiredService<ILogger<CommandSender>>()));

            // Add an instance of the Worker classes as the hosted services (1 engine, 2 stream workers, 1 stream queue worker, 1 command worker)
            builder.Services.AddHostedService<ModbusEngineWorker>(
                ew => new ModbusEngineWorker(
                    ew.GetRequiredService<Engine>(),
                    ew.GetRequiredService<ConfigState>(),
                    ew.GetRequiredService<PollControl>(),
                    ew.GetRequiredService<ILogger<ModbusEngineWorker>>(),
                    ew.GetRequiredService<IHostApplicationLifetime>()));
            builder.Services.AddHostedService<StreamPublisherWorker<ModbusDataDTO>>(
                spm => new StreamPublisherWorker<ModbusDataDTO>(
                    NamingConstants.ModbusDataStreamName,
                    spm.GetRequiredService<INamedPipeFactory>(),
                    spm.GetRequiredService<ConfigState>(),
                    spm.GetRequiredService<InitStatus>(),
                    spm.GetRequiredService<StreamQueue<ModbusDataDTO>>(),
                    spm.GetRequiredService<StreamPublisher<ModbusDataDTO>>(),
                    spm.GetRequiredService<ILogger<StreamPublisherWorker<ModbusDataDTO>>>(),
                    spm.GetRequiredService<IHostApplicationLifetime>()));
            builder.Services.AddHostedService<StreamPublisherWorker<ConnDiagDTO>>(
                spc => new StreamPublisherWorker<ConnDiagDTO>(
                    NamingConstants.ConnDiagStreamName,
                    spc.GetRequiredService<INamedPipeFactory>(),
                    spc.GetRequiredService<ConfigState>(),
                    spc.GetRequiredService<InitStatus>(),
                    spc.GetRequiredService<StreamQueue<ConnDiagDTO>>(),
                    spc.GetRequiredService<StreamPublisher<ConnDiagDTO>>(),
                    spc.GetRequiredService<ILogger<StreamPublisherWorker<ConnDiagDTO>>>(),
                    spc.GetRequiredService<IHostApplicationLifetime>()));
            builder.Services.AddHostedService<CommandsWorker>(
                cw => new CommandsWorker(
                    cw.GetRequiredService<ConfigState>(),
                    cw.GetRequiredService<CommandSender>(),
                    cw.GetRequiredService<CommandReceiver>(),
                    cw.GetRequiredService<PollControl>(),
                    cw.GetRequiredService<InitStatus>(),
                    cw.GetRequiredService<ILogger<CommandsWorker>>(),
                    cw.GetRequiredService<IHostApplicationLifetime>()));

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