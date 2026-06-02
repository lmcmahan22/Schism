// <copyright file="CommandsWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Schiism.Core;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Abstractions.RuntimeControl;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.Core.Models.RuntimeControl;
    using Schiism.Service.Implementations.RuntimeControl;
    using System.Diagnostics;
    using System.ServiceProcess;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Worker class for the backend. Handles SettingsConfig command logic in both directions (sending and receiving).
    /// </summary>
    /// <param name="receive">The command receiver, DI'd.</param>
    /// <param name="initSender">The initializing command sender, DI'd.</param>
    /// <param name="config">The current Modbus settings configuration.</param>
    /// <param name="control">The Modbus Control wrapper, used to control engine restarts.</param>
    /// <param name="fEInitState"> The Frontend status wrapper, used to determine if the Initializing command needs to be sent.</param>
    /// <param name="logger">Logger object used to write data to a text file.</param>
    public class ServiceCommandsWorker(ICommandReceiver receiver, ICommandSender initSender, IConfigState config, IModbusControl control, IInitializedState fEInitState, IServiceSettingsStore startupSettings, ILogger<ServiceCommandsWorker> logger, IHostApplicationLifetime lifetime) : BackgroundService
    {
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Don't run this worker until the application is fully started (i.e. all Worker's StartAsync() methods are complete).
            await Task.Run(
                () => lifetime.ApplicationStarted.WaitHandle.WaitOne(), stoppingToken);

            logger.LogInformation($"Service Commands Worker for {NamingConstants.SettingsCommandName} and {NamingConstants.InitSettingsCommandName} has started");

            Task? sendTask = this.RunSenderLoopAsync(stoppingToken);
            Task? receiveTask = this.RunReceiverLoopAsync(stoppingToken);

            await Task.WhenAll(receiveTask, sendTask);
        }

        private async Task RunSenderLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!fEInitState.IsInitialized)
                    {
                        // Send the configuration from the save file for intialization.
                        ServiceRuntimeSettings srs = startupSettings.Load();

                        SettingsConfig settConf = new SettingsConfig(
                            config.IPAddress,
                            config.DataLength,
                            config.StartAddress,
                            config.TCPPort,
                            config.ScanRate,
                            config.TCPTimeout,
                            config.DeviceId,
                            config.SelectedDataSize,
                            config.SelectedPollType,
                            config.AsciiEnable,
                            config.SelectedNumericBase,
                            config.SelectedEndian,
                            srs.AutoStart,
                            srs.AutoRestart);

                        logger.LogInformation("Sending initialization command");
                        await initSender.SendAsync(settConf, stoppingToken);

                        fEInitState.IsInitialized = true; // Ensure the state is set to true after successful send
                        logger.LogWarning("Frontend Initialization State set to True!");
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Init command send canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send init command. Trying again...");
                }

                await Task.Delay(1000, stoppingToken); // Wait a second before sending the init command again.
            }
        }

        private async Task RunReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Command receive starting");
                    await receiver.ReceiveAsync(this.ReceiveHandler, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Command receive crashed");
                }

                await Task.Delay(100, stoppingToken); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }

        private Task ReceiveHandler(SettingsConfig cmd)
        {
            config.Update(cmd);

            ServiceRuntimeSettings startSett = new ServiceRuntimeSettings();

            // Nullable sets
            if (cmd.AutoStart.HasValue)
            {
                startSett.AutoStart = cmd.AutoStart.Value;

                // Update in real time as well (if possible)
                ConfigureStartup(startSett.AutoStart);
            }

            if (cmd.AutoRestart.HasValue)
            {
                startSett.AutoRestart = cmd.AutoRestart.Value;

                // Update in real time as well (if possible)
                ConfigureRestart(startSett.AutoRestart);
            }

            // Save parameters, so they can be loaded on next boot
            startupSettings.Save(startSett);

            control.RestartRequested = true;

            logger.LogInformation("Implemented configuration command successfully.");
            return Task.CompletedTask;
        }

        private  void ConfigureStartup(bool enableAutoStart)
        {
            {
                // "delayed-auto" also works in place of "auto" here, but took about 90 seconds longer on my desktop PC to start up. Keeping this as "auto" until I see reason to change it.
                string startType = enableAutoStart ? "auto" : "demand";
                RunSc($"config {NamingConstants.ServiceName} start= {startType}");
            }
        }

        private void ConfigureRestart(bool enableRestart)
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
