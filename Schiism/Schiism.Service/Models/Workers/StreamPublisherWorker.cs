using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.DTOs.IPC_Records.Streams;

namespace Schiism.Service.Models.Workers
{
    /// <summary>
    /// Publishing Worker. Both ModbusData and ConnectionDiagnostics are published through this same worker type, just seperate instances.
    /// </summary>
    /// <typeparam name="T">The type of the stream item, either ModbusData or ConnectionDiagnostics.</typeparam>
    public class StreamPublisherWorker<T>(IStreamQueue<T> queue, IStreamPublisher<T> publisher, ILogger<StreamPublisherWorker<T>> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(
                "Starting stream publisher worker for {Type}",
                typeof(T).Name);

            await publisher.StartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var item = await queue.DequeueAsync(stoppingToken);

                    logger.LogInformation("Publishing {Type}", typeof(T).Name);
                    await publisher.PublishAsync(item, stoppingToken);
                    logger.LogInformation("Published {Type}:", typeof(T).Name);

                    if (item is ModbusData modbusData)
                    {
                        string data = string.Empty;
                        for (int i = 0; i < modbusData.Data.Count; i++)
                            {
                            data += $"Data[{i}]: {modbusData.Data[i]} ";
                        }

                        logger.LogInformation(
                            "Device: {DeviceId}, Data: {Data}",
                            modbusData.DeviceId,
                            data);
                    }
                    else if (item is ConnectionDiagnostics diagnostics)
                    {
                        logger.LogInformation(
                            "NumRequests: {NumRequests}, NumResponses: {NumResponses}, NumOKs: {NumOKs}, NumErrors: {NumErrors}, IsConnected: {IsConnected}, ErrorMessage: {ErrorMessage}, Timestamp: {Timestamp}",
                            diagnostics.NumRequests,
                            diagnostics.NumResponses,
                            diagnostics.NumOKs,
                            diagnostics.NumErrors,
                            diagnostics.IsConnected,
                            diagnostics.ErrorMessage,
                            diagnostics.Timestamp);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogError(
                    "Received CancellationToken, stopping worker for {Type}",
                    typeof(T).Name);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error publishing stream item for {Type}",
                        typeof(T).Name);
                }
            }

            logger.LogInformation(
                "Stopping stream publisher worker for {Type}",
                typeof(T).Name);
        }
    }
}
