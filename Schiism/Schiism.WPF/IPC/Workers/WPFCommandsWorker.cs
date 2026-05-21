using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schiism.Core.Abstractions.IPC.Commands;
using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Models.IPC.DTOs.Commands;
using Schiism.WPF.Models.Implementations.States;

namespace Schiism.WPF.IPC.Workers
{
    /// <summary>
    /// Directly mirrors the CommandsWorker class from the Service project, using the same BackgroundService interface to run this on a seperate thread.
    /// </summary>
    public class WPFCommandsWorker : BackgroundService
    {
        // No in-line constructor here, in order to support the logger:
        private readonly ILogger logger;
        private readonly ICommandReceiver initReceiver;
        private readonly ICommandSender sender;
        private readonly IWPFConfigState configSettState;
        private readonly WPFInitializedState initState;

        // Track if the config actually needs to be sent or not.
        private SettingsConfig? lastSentConfig;

        public WPFCommandsWorker(ICommandReceiver initReceiver, ICommandSender sender, IWPFConfigState configSettState, WPFInitializedState initState, ILoggerFactory factory)
        {
            this.logger = factory.CreateLogger<WPFCommandsWorker>();
            this.initReceiver = initReceiver;
            this.sender = sender;
            this.configSettState = configSettState;
            this.initState = initState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task? sendTask = this.RunSenderLoopAsync(stoppingToken);
            Task? receiveTask = this.RunReceiverLoopAsync(stoppingToken);

            await Task.WhenAll(sendTask, receiveTask);
        }

        private async Task? RunSenderLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (initState.IsInitialized)
                    {
                        // Make this so it only sends UPDATED parameters, rather than everything every single time!
                        SettingsConfig currentConfig = new SettingsConfig(
                            configSettState.IPAddress,
                            configSettState.DataLength,
                            configSettState.StartAddress,
                            configSettState.TCPPort,
                            configSettState.ScanRate,
                            configSettState.TCPTimeout,
                            configSettState.DeviceId,
                            configSettState.SelectedDataSize,
                            configSettState.SelectedPollType,
                            configSettState.AsciiEnable,
                            configSettState.SelectedNumericBase,
                            configSettState.SelectedEndian);

                        if (!currentConfig.Equals(lastSentConfig))
                        {
                            logger.LogInformation("Settings changed, sending update");

                            await sender.SendAsync(currentConfig, stoppingToken);

                            logger.LogInformation("Updated settings sent!");

                            lastSentConfig = currentConfig;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogError("Command send cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to send init command");
                    throw;
                }

                await Task.Delay(1000, stoppingToken); // This makes it so Settings can only be sent as fast as once per second.
                                                       // I think that's appropriate for now, assuming the user can make changes that quickly.
            }
        }

        private async Task? RunReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!initState.IsInitialized)
                    {
                        logger.LogInformation("Initializing command receive starting");
                        await initReceiver.ReceiveAsync(this.ReceiveHandler, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Initialization command receive crashed. Trying again...");
                    throw;
                }

                await Task.Delay(100, stoppingToken); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }

        private Task ReceiveHandler(SettingsConfig cmd)
        {
            // Trigger a PropertyChanged event to notify the view model for a UI update
            configSettState.Update(cmd);

            initState.IsInitialized = true;
            logger.LogInformation("Initialization State set to True!");

            logger.LogInformation("Implemented initializing configuration command successfully");
            return Task.CompletedTask;
        }
    }
}
