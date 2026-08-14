namespace Schiism.WPF.IPC
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.Commands;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.WPF.Models;
    using System.ComponentModel;

    /// <summary>
    /// Directly mirrors the CommandsWorker class from the Service project, using the same BackgroundService interface to run this on a seperate thread.
    /// </summary>
    public class CommandsWorker : BackgroundService
    {
        // No in-line constructor here, in order to support the logger:
        private readonly ILogger<CommandsWorker> logger;
        private readonly CommandReceiver<SettingsConfigDTO> initConfigReceiver;
        private readonly CommandSender<SettingsConfigDTO> configSender;
        private readonly CommandSender<ModbusWriteDTO> modbusSender;
        private readonly ConfigState configState;
        private readonly ModbusWriteState writeState;
        private readonly InitStatus initStatus;

        // Track if the config actually needs to be sent or not.
        private SettingsConfigDTO? lastSentConfig;

        public CommandsWorker(
            CommandReceiver<SettingsConfigDTO> initConfigReceiver,
            INamedPipeFactory pipeFactory,
            CommandSender<SettingsConfigDTO> configSender,
            CommandSender<ModbusWriteDTO> modbusSender,
            ConfigState configState,
            ModbusWriteState writeState,
            InitStatus initStatus,
            ILogger<CommandsWorker> logger)
        {
            this.logger = logger;
            this.initConfigReceiver = initConfigReceiver;
            this.configSender = configSender;
            this.modbusSender = modbusSender;
            this.configState = configState;
            this.writeState = writeState;
            this.initStatus = initStatus;

            // Sender subscriptions (complete this by binding to the WPF element with the data!)
            this.configState.PropertyChanged += ConfigChanged;
            this.writeState.PropertyChanged += ValueChanged;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Sender is handled via configState subscription.
            await RunConfigReceiverLoopAsync(stoppingToken);
        }

        private async Task? RunConfigReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!initStatus.IsInitialized)
                    {
                        logger.LogInformation("Initializing command receive starting");
                        await initConfigReceiver.ReceiveAsync(ReceiveHandler, stoppingToken);
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

        private async void ConfigChanged(object? configSendObject, PropertyChangedEventArgs e)
        {
            if (!initStatus.IsInitialized)
            {
                return;
            }

            try
            {
                // length is included here, so the UI can view the length, but it isn't able to set it
                SettingsConfigDTO currentConfig = new SettingsConfigDTO(
                    configState.IPAddress,
                    configState.DataLength,
                    configState.StartAddress,
                    configState.TCPPort,
                    configState.ScanRate,
                    configState.TCPTimeout,
                    configState.DeviceId,
                    configState.SelectedDataSize,
                    configState.SelectedPollType,
                    configState.AsciiEnable,
                    configState.SelectedNumericBase,
                    configState.SelectedEndian,
                    configState.AutoStart,
                    configState.AutoRestart);

                if (currentConfig.Equals(lastSentConfig))
                {
                    return;
                }

                logger.LogInformation("Settings changed, sending update");

                await this.configSender.SendAsync(currentConfig, CancellationToken.None);

                lastSentConfig = currentConfig;

                logger.LogInformation("Updated settings sent!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send config update");
            }
        }

        private async void ValueChanged(object? modbusSenderObject, PropertyChangedEventArgs e)
        {
            if (!initStatus.IsInitialized)
            {
                return;
            }

            try
            {
                // Y is the value to write from the UI. Call value change with respect to cell X.
                ModbusWriteDTO modbusWriteDTO = new ModbusWriteDTO(
                    configState.SelectedPollType,
                    configState.DeviceId,
                    writeState.Address,
                    writeState.Value);

                logger.LogInformation("Sending new value " + writeState.Value + " to address: " + writeState.Address);

                await this.modbusSender.SendAsync(modbusWriteDTO, CancellationToken.None);

                logger.LogInformation("Value sent successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send value!");
            }
        }

        private Task ReceiveHandler(SettingsConfigDTO cmd)
        {
            // Trigger a PropertyChanged event to notify the view model for a UI update
            configState.Update(cmd);

            initStatus.IsInitialized = true;
            logger.LogInformation("Initialization State set to True!");

            logger.LogInformation("Implemented initializing configuration command successfully");
            return Task.CompletedTask;
        }
    }
}
