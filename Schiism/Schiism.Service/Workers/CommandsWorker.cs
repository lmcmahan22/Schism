// <copyright file="CommandsWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// Worker class for the backend. Handles SettingsConfig command logic in both directions (sending and receiving).
    /// </summary>
    /// <param name="receive">The command receiver, DI'd.</param>
    /// <param name="initSender">The initializing command sender, DI'd.</param>
    /// <param name="config">The current Modbus settings configuration.</param>
    /// <param name="control">The Modbus Control wrapper, used to control engine restarts.</param>
    /// <param name="fEInitState"> The Frontend status wrapper, used to determine if the Initializing command needs to be sent.</param>
    /// <param name="logger">Logger object used to write data to a text file.</param>
    public class CommandsWorker(ICommandReceiver receive, ICommandSender initSender, IModbusConfig config, IModbusControl control, ILoadConfigState fEInitState, ILogger<CommandsWorker> logger) : BackgroundService
    {
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task? sendTask = this.RunSenderLoopAsync(stoppingToken);
            Task? receiveTask = this.RunReceiverAsync(stoppingToken);

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
                            config.SelectedEndian);

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
                    // fEInitState.SetConnected(false);
                    logger.LogError(ex, "Failed to send init command. Trying again...");
                }

                await Task.Delay(2000, stoppingToken); // IMPORTANT or you'll spin CPU
            }
        }

        private async Task RunReceiverAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Command receive starting");
                await receive.ReceiveAsync(this.ServiceCommandsHandler, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Command receive crashed");
            }
        }

        private Task ServiceCommandsHandler(SettingsConfig cmd)
        {
            config.Update(cmd); // or map fields manually
            control.RestartRequested = true;
            logger.LogInformation("Implemented configuration command successfully.");
            return Task.CompletedTask;
        }
    }
}
