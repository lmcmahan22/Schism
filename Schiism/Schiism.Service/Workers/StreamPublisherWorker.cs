// <copyright file="StreamPublisherWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using Schiism.Core.Abstractions.IPC;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC.DTOs.Streams;

    /// <summary>
    /// Publishing Worker. Both ModbusData and ConnectionDiagnostics are published through this same worker type, just seperate instances.
    /// </summary>
    /// <typeparam name="T">The type of the stream item, either ModbusData or ConnectionDiagnostics.</typeparam>
    public class StreamPublisherWorker<T>(IStreamQueue<T> queue, IStreamPublisher<T> publisher, ILogger<StreamPublisherWorker<T>> logger) : BackgroundService
    {
        /// <inheritdoc/>
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
                    T? item = await queue.DequeueAsync(stoppingToken);

                    if (publisher.IsConnected)
                    {
                        await publisher.PublishAsync(item, stoppingToken);

                        if (item is ModbusData modbusData)
                        {
                            string data = string.Empty;
                            for (int i = 0; i < modbusData.Data.Count; i++)
                            {
                                data += $"Data[{i}]: {modbusData.Data[i]} ";
                            }

                            logger.LogInformation(
                                "Published {Type}: Data: {Data}",
                                typeof(T).Name,
                                data);
                        }
                    }

                    // else if (item is ConnectionDiagnostics diagnostics)
                    // {
                    //    logger.LogInformation(
                    //        "Published {Type}: NumRequests: {NumRequests}, NumResponses: {NumResponses}, NumOKs: {NumOKs}, NumErrors: {NumErrors}, IsConnected: {IsConnected}, ErrorMessage: {ErrorMessage}, Timestamp: {Timestamp}",
                    //        typeof(T).Name,
                    //        diagnostics.NumRequests,
                    //        diagnostics.NumResponses,
                    //        diagnostics.NumOKs,
                    //        diagnostics.NumErrors,
                    //        diagnostics.IsConnected,
                    //        diagnostics.ErrorMessage,
                    //        diagnostics.Timestamp);
                    // }
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
                    logger.LogError(
                        ex,
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
