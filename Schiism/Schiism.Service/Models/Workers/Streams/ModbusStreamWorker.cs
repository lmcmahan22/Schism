using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.DTOs.IPC_Records.Streams;

namespace Schiism.Service.Models.Workers.Streams
{
    public class ModbusStreamWorker(IStreamQueue<ModbusData> queue, IStreamPublisher<ModbusData> publisher, ILogger<ModbusStreamWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var data in queue.ReadAllAsync(stoppingToken))
                {
                    var lag = DateTime.UtcNow - data.Timestamp;

                    logger.LogInformation(
                        "MODBUS Stream | Device={DeviceId} Data={Data} Lag={LagMs}ms",
                        data.DeviceId,
                        data.Data,
                        lag.TotalMilliseconds);

                    await publisher.PublishAsync(data, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"ModbusStreamWorker crashed: {ex}");
                throw;
            }
        }
    }
}
