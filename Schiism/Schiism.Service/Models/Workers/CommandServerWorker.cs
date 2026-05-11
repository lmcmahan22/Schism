namespace Schiism.Service.Models.Workers
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.DTOs.IPC_Records.Commands;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class CommandServerWorker(ICommandServer<SettingsConfig> server, IModbusConfig modbusConfig, IModbusControl modbusControl, ILogger<CommandServerWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Starting command server worker");
                await server.HandleClient(this.HandleCommandAsync, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Command server worker crashed");
            }
        }

        private async Task HandleCommandAsync(SettingsConfig cmd)
        {
            logger.LogInformation("Received MODBUS config command");

            // Apply settings and restart MODBUS engine
            modbusConfig.Update(cmd); // or map fields manually
            modbusControl.RequestRestart();
        }
    }
}
