namespace Schiism.Service.Models.Workers.Streams
{
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.DTOs.IPC_Records.Streams;

    public class QueueMonitorWorker(IStreamQueue<ModbusData> modbusDataQueue, IStreamQueue<ConnectionDiagnostics> connDiagQueue, ILogger<QueueMonitorWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    var m = modbusDataQueue.Snapshot();
                    var c = connDiagQueue.Snapshot();

                    logger.LogInformation(
                        "MODBUS Queue | Depth={Depth} Enqueued={Enq} Processed={Proc} Dropped={Drop} Lag={LagMs}ms",
                        m.CurrentDepth,
                        m.TotalEnqueued,
                        m.TotalProcessed,
                        m.Dropped,
                        (DateTime.UtcNow - m.LastDequeueTime).TotalMilliseconds);
                    logger.LogInformation(
                        "Diagnostics Queue | Depth={Depth} Enqueued={Enq} Processed={Proc} Dropped={Drop} Lag={LagMs}ms",
                        c.CurrentDepth,
                        c.TotalEnqueued,
                        c.TotalProcessed,
                        c.Dropped,
                        (DateTime.UtcNow - c.LastDequeueTime).TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, $"QueueMonitorWorker crashed: {ex}");
                throw; // or swallow depending on strategy
            }
        }
    }
}
