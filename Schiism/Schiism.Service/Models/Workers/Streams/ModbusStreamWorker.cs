using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.DTOs.IPC.Streams;

namespace Schiism.Service.Models.Workers.Streams
{
    public class ModbusStreamWorker(IStreamQueue<ModbusData> queue, IStreamPublisher<ModbusData> publisher, ILogger<ModbusStreamWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var data in queue.ReadAllAsync(stoppingToken))
            {
                var lag = DateTime.UtcNow - data.Timestamp;

                logger.LogInformation(
                    "MODBUS Stream | Device={DeviceId} Lag={LagMs}ms",
                    data.DeviceId,
                    lag.TotalMilliseconds);

                await publisher.PublishAsync(data, stoppingToken);
            }
        }
    }
}
