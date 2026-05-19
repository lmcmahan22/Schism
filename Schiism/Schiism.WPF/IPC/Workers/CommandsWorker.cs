using Microsoft.Extensions.Hosting;
using Schiism.Core.Abstractions.IPC.Commands;
using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Models.IPC.DTOs.Commands;

namespace Schiism.WPF.IPC.Workers
{
    /// <summary>
    /// Directly mirrors the CommandsWorker class from the Service project, using the same BackgroundService interface to run this on a seperate thread.
    /// </summary>
    public class CommandsWorker(ICommandReceiver initReceiver, ICommandSender sender, IConfigState configSettState, IInitializedState initState) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task? sendTask = this.RunSenderAsync(stoppingToken);
            Task? receiveTask = this.RunReceiverLoopAsync(stoppingToken);

            await Task.WhenAll(sendTask, receiveTask);
        }

        private async Task? RunSenderAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (initState.IsInitialized)
                {
                    // Make this so it only sends UPDATED parameters, rather than everything every single time!
                    SettingsConfig settConf = new SettingsConfig(
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

                    // "Command Send starting"

                    await sender.SendAsync(settConf, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // "Command send cancelled"
            }
            catch (Exception ex)
            {
                // "Failed to send init command. Try again."
            }
        }

        private async Task? RunReceiverLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!initState.IsInitialized)
                {
                    try
                    {
                        // "Init Command receive starting"
                        await initReceiver.ReceiveAsync(this.ReceiveHandler, stoppingToken);
                        initState.IsInitialized = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        // "Command receive crashed"
                    }

                    await Task.Delay(2000, stoppingToken); // IMPORTANT or you'll spin CPU
                }
            }
        }

        private Task ReceiveHandler(SettingsConfig cmd)
        {
            // Trigger a PropertyChanged event to notify the view model for a UI update
            configSettState.Update(cmd);

            // "Implemented configuration command successfully."
            return Task.CompletedTask;
        }
    }
}
