using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.DTOs.IPC_Records.Streams;

namespace Schiism.Service.Models.Workers.Streams
{
    public class ConnDiagStreamNameWorker(IStreamQueue<ConnectionDiagnostics> queue, IStreamPublisher<ConnectionDiagnostics> publisher, ILogger<ConnDiagStreamNameWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var info in queue.ReadAllAsync(stoppingToken))
                {
                    var lag = DateTime.UtcNow - info.Timestamp;

                    logger.LogInformation(
                        "Diagnostics Stream | Lag={LagMs}ms",
                        lag.TotalMilliseconds);

                    await publisher.PublishAsync(info, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"ConnDiagStreamWorker crashed: {ex}");
                throw;
            }
        }
    }
}
