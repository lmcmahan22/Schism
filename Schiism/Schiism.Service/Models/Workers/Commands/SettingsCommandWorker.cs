
namespace Schiism.Service.Models.Workers.Commands
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.DTOs.IPC_Records.Commands;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    public class SettingsCommandNameWorker(ICommandServer<SettingsConfig> server, IModbusConfig modbusConfig, ModbusEngineWorker modbusEngineWorker, ILogger<SettingsCommandNameWorker> logger) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await server.StartAsync(this.HandleCommandAsync, stoppingToken);
        }

        private async Task HandleCommandAsync(SettingsConfig cmd)
        {
            logger.LogInformation("Received MODBUS config command");

            // Apply settings and restart MODBUS engine
            modbusConfig.Update(cmd); // or map fields manually
            await modbusEngineWorker.RequestRestart();
        }
    }
}
