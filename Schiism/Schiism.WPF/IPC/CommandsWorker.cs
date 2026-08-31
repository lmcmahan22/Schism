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
    using Schiism.Core.Configuration.Enums;
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
        private readonly CommandSender<BoardAvailableDTO> boardAvailableSender;
        private readonly ConfigState configState;
        private readonly ModbusWriteState writeState;
        private readonly BoardAvailableState bAState;
        private readonly InitStatus initStatus;

        // Track if the config actually needs to be sent or not.
        private SettingsConfigDTO? lastSentConfig;

        public CommandsWorker(
            CommandReceiver<SettingsConfigDTO> initConfigReceiver,
            INamedPipeFactory pipeFactory,
            CommandSender<SettingsConfigDTO> configSender,
            CommandSender<ModbusWriteDTO> modbusSender,
            CommandSender<BoardAvailableDTO> boardAvailableSender,
            ConfigState configState,
            ModbusWriteState writeState,
            BoardAvailableState bAState,
            InitStatus initStatus,
            ILogger<CommandsWorker> logger)
        {
            this.logger = logger;
            this.initConfigReceiver = initConfigReceiver;
            this.configSender = configSender;
            this.modbusSender = modbusSender;
            this.boardAvailableSender = boardAvailableSender;
            this.configState = configState;
            this.writeState = writeState;
            this.bAState = bAState;
            this.initStatus = initStatus;

            // Sender subscriptions (complete this by binding to the WPF element with the data!)
            this.configState.MSSendTrigger += MSSendTrigger;
            this.writeState.WriteSendTrigger += WriteSendTrigger;
            this.bAState.BASendTrigger += BASendTrigger;
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
                        logger.LogInformation("[WPF] Initialization command receive starting");
                        await initConfigReceiver.ReceiveAsync(InitSettHandler, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("[WPF] Initialization command receive crashed. Trying again...");
                    throw;
                }

                await Task.Delay(100, stoppingToken); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }

        private async void MSSendTrigger(object? configSendObject, EventArgs e)
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

                logger.LogInformation("[WPF] Settings changed, sending update on {0}", nameof(this.configSender));

                await this.configSender.SendAsync(currentConfig, CancellationToken.None);

                lastSentConfig = currentConfig;

                logger.LogInformation("[WPF] Updated settings sent on {0}!", nameof(this.configSender));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WPF] Failed to send config update on {0}", nameof(this.configSender));
            }
        }

        private async void BASendTrigger(object? bAStateSenderObject, EventArgs e)
        {
            try
            {
                // send as a bit (0,1,2) instead, based on the enum value.
                byte fail, flip;

                switch (bAState.FailedBoard)
                {
                    case FailType.Good:
                        fail = 1;
                        break;

                    case FailType.Failed:
                        fail = 2;
                        break;

                    default:
                        fail = 0;
                        break;
                }

                switch (bAState.FlippedBoard)
                {
                    case FlipType.NotFlipped:
                        flip = 1;
                        break;

                    case FlipType.Flipped:
                        flip = 2;
                        break;

                    default:
                        flip = 0;
                        break;
                }

                BoardAvailableDTO boardAvailableDTO = new BoardAvailableDTO(
                    bAState.BoardID,
                    bAState.Width,
                    fail,
                    flip,
                    bAState.ReceiptDir,
                    bAState.TopBarcode,
                    bAState.BottomBarcode,
                    bAState.PartName);

                await this.boardAvailableSender.SendAsync(boardAvailableDTO, CancellationToken.None);

                logger.LogInformation("[WPF] BoardAvailable sent sucessfully on {0}!", nameof(this.boardAvailableSender));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WPF] Failed to send BoardAvailable message on {0}.", nameof(this.boardAvailableSender));
            }
        }

        private async void WriteSendTrigger(object? modbusSenderObject, EventArgs e)
        {
            try
            {
                // Value handling with different string value formats
                string cleanedVal = string.Empty;

                switch (configState.SelectedNumericBase)
                {
                    case NumericBase.Integer:
                        cleanedVal = ((ushort)Convert.ToInt16(this.writeState.Value, 10)).ToString();
                        break;

                    case NumericBase.Binary:
                        cleanedVal = Convert.ToUInt16(this.writeState.Value, 2).ToString();
                        break;

                    case NumericBase.Hexadecimal:
                        string tempVal = this.writeState.Value;
                        if (this.configState.AsciiEnable)
                        {
                            // Remove first 5 characters (ASCII content)
                            tempVal = tempVal.Substring(5);
                        }

                        // Remove first 2 characters ("0x")
                        cleanedVal = Convert.ToUInt16(tempVal.Substring(2), 16).ToString();
                        break;

                    // Not implemented for the time being...
                    // case NumericBase.Float:
                    //    // Convert
                    //    break;

                    default:
                        cleanedVal = this.writeState.Value;
                        break;
                }

                ModbusWriteDTO modbusWriteDTO = new ModbusWriteDTO(
                    writeState.SelectedPollType,
                    configState.DeviceId,
                    writeState.Address,
                    cleanedVal);

                logger.LogInformation("[WPF] Sending new value " + writeState.Value + " to address: " + writeState.Address + " on {0}", nameof(this.modbusSender));

                await this.modbusSender.SendAsync(modbusWriteDTO, CancellationToken.None);

                logger.LogInformation("[WPF] Value sent successfully on {0}", nameof(this.modbusSender));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WPF] Failed to send value on {0}. Error details: {1}", nameof(this.modbusSender), ex.Message);
            }
        }

        private Task InitSettHandler(SettingsConfigDTO cmd)
        {
            // Trigger a PropertyChanged event to notify the view model for a UI update
            configState.Update(cmd);
            logger.LogInformation("[WPF] Implemented initializing configuration command successfully");

            initStatus.IsInitialized = true;
            logger.LogInformation("[WPF] Initialization State set to True!");

            return Task.CompletedTask;
        }
    }
}
