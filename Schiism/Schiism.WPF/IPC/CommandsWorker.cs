namespace Schiism.WPF.IPC
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.Commands;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.StateWrappers;
    using System.ComponentModel;

    /// <summary>
    /// Directly mirrors the CommandsWorker class from the Service project, using the same BackgroundService interface to run this on a seperate thread.
    /// </summary>
    public class CommandsWorker : BackgroundService
    {
        // No in-line constructor here, in order to support the logger:
        private readonly ILogger<CommandsWorker> logger;
        private readonly CommandReceiver initReceiver;
        private readonly CommandSender sender;
        private readonly ConfigState config;
        private readonly InitStatus initStatus;

        // Track if the config actually needs to be sent or not.
        private SettingsConfig? lastSentConfig;

        public CommandsWorker(
            CommandReceiver initReceiver,
            INamedPipeFactory pipeFactory,
            CommandSender sender,
            ConfigState config,
            InitStatus initStatus,
            ILogger<CommandsWorker> logger)
        {
            this.logger = logger;
            this.initReceiver = initReceiver;
            this.sender = sender;
            this.config = config;
            this.initStatus = initStatus;

            // Sender subscription
            this.config.PropertyChanged += ConfigChanged;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Sender is handled via configState subscription.
            await RunReceiverLoopAsync(stoppingToken);
        }

        private async Task? RunReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!initStatus.IsInitialized)
                    {
                        logger.LogInformation("Initializing command receive starting");
                        await initReceiver.ReceiveAsync(ReceiveHandler, stoppingToken);
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

        private async void ConfigChanged(object? configSender, PropertyChangedEventArgs e)
        {
            if (!initStatus.IsInitialized)
            {
                return;
            }

            try
            {
                // length is included here, so the UI can view the length, but it isn't able to set it
                SettingsConfig currentConfig = new SettingsConfig(
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
                    config.AutoStart,
                    config.AutoRestart);

                if (currentConfig.Equals(lastSentConfig))
                {
                    return;
                }

                logger.LogInformation("Settings changed, sending update");

                await sender.SendAsync(currentConfig, CancellationToken.None);

                lastSentConfig = currentConfig;

                logger.LogInformation("Updated settings sent!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send config update");
            }
        }

        private Task ReceiveHandler(SettingsConfig cmd)
        {
            // Trigger a PropertyChanged event to notify the view model for a UI update
            config.Update(cmd);

            initStatus.IsInitialized = true;
            logger.LogInformation("Initialization State set to True!");

            logger.LogInformation("Implemented initializing configuration command successfully");
            return Task.CompletedTask;
        }
    }
}
