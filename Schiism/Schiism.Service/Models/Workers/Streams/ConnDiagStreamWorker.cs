using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.DTOs.IPC.Streams;

namespace Schiism.Service.Models.Workers.Streams
{
    public class ConnDiagStreamWorker(IStreamQueue<ConnectionDiagnostics> queue, IStreamPublisher<ConnectionDiagnostics> publisher, ILogger<ConnDiagStreamWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
    }
}
