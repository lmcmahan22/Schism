// <copyright file="StreamPublisherWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using System.IO.Pipes;

    /// <summary>
    /// Publishing Worker. Both ModbusData and ConnectionDiagnostics are published through this same worker type, just seperate instances.
    /// </summary>
    /// <typeparam name="T">The type of the stream item, either ModbusData or ConnectionDiagnostics.</typeparam>
    public class StreamPublisherWorker<T>(string pipeName, IInitializedState fEInitState, IStreamQueue<T> queue, IStreamPublisher<T> publisher, ILogger<StreamPublisherWorker<T>> logger) : BackgroundService
    {
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && fEInitState.IsInitialized)
            {
                NamedPipeServerStream? pipe = null;

                try
                {
                    pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.Out,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    logger.LogInformation($"Waiting for client on {pipeName}");

                    await pipe.WaitForConnectionAsync(stoppingToken);

                    logger.LogInformation($"Client connected on {pipeName}");

                    while (!stoppingToken.IsCancellationRequested && pipe.IsConnected)
                    {
                        T? item = await queue.DequeueAsync(stoppingToken);

                        logger.LogInformation($"Publishing on {pipeName}");
                        await publisher.PublishAsync(pipe, item, stoppingToken);
                        logger.LogInformation($"Publish complete on {pipeName}");

                        await Task.Delay(2000, stoppingToken);
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
                    logger.LogError(
                        ex,
                        "Error publishing stream item for {Type}",
                        typeof(T).Name);
                }
                finally
                {
                    pipe?.Dispose();
                }
            }

            logger.LogInformation(
                "Stopping stream publisher worker for {Type}",
                typeof(T).Name);
        }
    }
}
