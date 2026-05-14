namespace Schiism.Service.Workers
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.Modbus;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.Core.Abstractions.IPC;

    public class CommandsWorker(ICommandReceiver<SettingsConfig> receive, ICommandSender<SettingsConfig> initSender, IModbusConfig modbusConfig, IModbusControl modbusControl, IFrontendInitState fEInitState, ILogger<CommandsWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sendTask = this.RunSenderLoopAsync(stoppingToken);
            var receiveTask = this.RunReceiverAsync(stoppingToken);

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
                        logger.LogInformation("Sending initialization command");

                        var initConf = new SettingsConfig(
                            modbusConfig.IPAddress,
                            modbusConfig.DataLength,
                            modbusConfig.StartAddress,
                            modbusConfig.TCPPort,
                            modbusConfig.ScanRate,
                            modbusConfig.TCPTimeout,
                            modbusConfig.DeviceId,
                            modbusConfig.SelectedDataSize,
                            modbusConfig.SelectedPollType,
                            modbusConfig.AsciiEnable,
                            modbusConfig.SelectedNumericBase,
                            modbusConfig.SelectedEndian);

                        await initSender.SendAsync(initConf, this.HandleCommandSendAsync, stoppingToken);
                        fEInitState.SetInitialized(true);
                        logger.LogWarning("Frontend Initialization State set to True!");
                    }
                }
                catch (OperationCanceledException)
                {
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
                await receive.ReceiveAsync(this.HandleCommandReceiptAsync, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Command receive crashed");
            }
        }

        private async Task HandleCommandSendAsync(SettingsConfig cmd)
        {
            logger.LogInformation("Front end received initial config successfully");
        }

        private async Task HandleCommandReceiptAsync(SettingsConfig cmd)
        {
            logger.LogInformation("Received MODBUS config command");

            // Apply settings and restart MODBUS engine
            modbusConfig.Update(cmd); // or map fields manually
            modbusControl.RequestRestart();
        }
    }
}
