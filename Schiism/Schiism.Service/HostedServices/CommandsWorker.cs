// <copyright file="CommandsWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.HostedServices
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Schiism.Core;
    using Schiism.Core.Configuration.FileControl;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.Commands;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.Modbus;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Worker class for the backend. Handles SettingsConfig command logic in both directions (sending and receiving).
    /// </summary>
    /// <param name="receive">The command receiver, DI'd.</param>
    /// <param name="initSender">The initializing command sender, DI'd.</param>
    /// <param name="config">The current Modbus settings configuration.</param>
    /// <param name="control">The Modbus Control wrapper, used to control engine restarts.</param>
    /// <param name="initStatus"> The Frontend status wrapper, used to determine if the Initializing command needs to be sent.</param>
    /// <param name="logger">Logger object used to write data to a text file.</param>
    public class CommandsWorker(
        ConfigState config,
        Engine engine,
        CommandSender<SettingsConfigDTO> initConfigSender,
        CommandReceiver<SettingsConfigDTO> configReceiver,
        CommandReceiver<ModbusWriteDTO> modbusWriteReceiver,
        CommandReceiver<BoardAvailableDTO> boardAvailableReceiver,
        PollControl pollControl,
        InitStatus initStatus,
        ILogger<CommandsWorker> logger,
        IHostApplicationLifetime lifetime)
        : BackgroundService
    {

        private ServiceSettingsStore servSett = new ServiceSettingsStore();

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Don't run this worker until the application is fully started (i.e. all Worker's StartAsync() methods are complete).
            await Task.Run(
                () => lifetime.ApplicationStarted.WaitHandle.WaitOne(), stoppingToken);

            logger.LogInformation($"Service Commands Worker for {NamingConstants.SettingsCommandName}, {NamingConstants.InitSettingsCommandName}, {NamingConstants.ModbusWriteCommandName}, and {NamingConstants.BoardAvailableCommandName} has started");

            Task? sendTask = RunInitSenderLoopAsync(stoppingToken);
            Task? settingsReceiveTask = RunSettingsReceiverLoopAsync(stoppingToken);
            Task? modbusWriteReceiveTask = RunModbusWriteReceiverLoopAsync(stoppingToken);
            Task? boardAvailableReceiveTask = RunBoardAvailableReceiverLoopAsync(stoppingToken);

            if (config.PLCHeartbeatEnable)
            {
                Task? heartbeatTask = PLCHeartbeatLoopAsync(stoppingToken);
                await Task.WhenAll(settingsReceiveTask, modbusWriteReceiveTask, heartbeatTask, boardAvailableReceiveTask, sendTask);
            }
            else
            {
                await Task.WhenAll(settingsReceiveTask, modbusWriteReceiveTask, boardAvailableReceiveTask, sendTask);
            }
        }

        private async Task RunInitSenderLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!initStatus.IsInitialized)
                    {

                        logger.LogInformation("Sending initialization command");
                        await initConfigSender.SendAsync(config.Push(), stoppingToken);

                        initStatus.IsInitialized = true; // Ensure the state is set to true after successful send
                        logger.LogInformation("Frontend Initialization State set to True!");
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

        private async Task RunSettingsReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Command receive starting");
                    await configReceiver.ReceiveAsync(this.SettingsReceiveHandler, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Command receive crashed");
                }

                await Task.Delay(100, stoppingToken); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }

        private async Task RunModbusWriteReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Modbus Write receive starting");
                    await modbusWriteReceiver.ReceiveAsync(this.ModbusWriteReceiveHandler, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Modbus Write receive crashed");
                }

                await Task.Delay(100, stoppingToken); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }

        private async Task RunBoardAvailableReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("BoardAvailable receive starting");
                    await boardAvailableReceiver.ReceiveAsync(this.BoardAvailableReceiveHandler, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BoardAvailable receive crashed");
                }

                await Task.Delay(100, stoppingToken); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }

        private async Task PLCHeartbeatLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await engine.PLCHeartbeatAsync(config);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Modbus Heartbeat crashed");
                }
                finally
                {
                    await Task.Delay(3000, stoppingToken);
                }
            }
        }

        private Task SettingsReceiveHandler(SettingsConfigDTO cmd)
        {
            config.Update(cmd);

            // Save parameters, so they can be loaded on next boot
            ServiceSaveData ssd = new ServiceSaveData(config.AutoStart, config.AutoRestart);
            this.servSett.Save(ssd);

            // Efficiency improvement, only run this if a setting other than the poll type was modified. We always poll both Status Coils and Holding Registers, so we no longer need to restart for that!
            pollControl.RestartRequested = true;

            logger.LogInformation("Implemented configuration command successfully.");
            return Task.CompletedTask;
        }

        private Task ModbusWriteReceiveHandler(ModbusWriteDTO write)
        {
            // Implement the logic to handle Modbus write to the Server device. Should just be a method with the DTO as the parameter.
            engine.WriteValueAsync(write, config);

            logger.LogInformation("Implemented Modbus Value: " + write.Value + " at " + write.Address + " successfully.");
            return Task.CompletedTask;
        }

        private Task BoardAvailableReceiveHandler(BoardAvailableDTO baDTO)
        {
            // Implement the logic to handle Modbus write to the Server device. Should just be a method with the DTO as the parameter.
            engine.WriteBoardAvailableAsync(baDTO, config);

            logger.LogInformation("Implemented BoardAvailable with PartName: " + baDTO.PartName + " successfully.");
            return Task.CompletedTask;
        }
    }
}
