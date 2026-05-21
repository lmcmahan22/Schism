// <copyright file="StreamPublisherWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC.DTOs.Streams;
    using System.IO.Pipes;

    /// <summary>
    /// Publishing Worker. Both ModbusData and ConnectionDiagnostics are published through this same worker type, just seperate instances.
    /// </summary>
    /// <typeparam name="T">The type of the stream item, either ModbusData or ConnectionDiagnostics.</typeparam>
    public class StreamPublisherWorker<T>(string pipeName, IConfigState config, IInitializedState fEInitState, IStreamQueue<T> queue, IStreamPublisher<T> publisher, ILogger<StreamPublisherWorker<T>> logger) : BackgroundService
    {
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!fEInitState.IsInitialized)
                {
                    // Wait a second for the WPF app to initialize
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

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

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        if (queue.Count == 0)
                        {
                            await Task.Delay(100, stoppingToken);
                            continue;
                        }

                        T? item = await queue.DequeueAsync(stoppingToken);

                        logger.LogInformation($"Publishing on {pipeName}");
                        await publisher.PublishAsync(pipe, item, stoppingToken);

                        if (item.GetType() == typeof(ModbusData))
                        {
                            string result = string.Empty;
                            ModbusData? modItem = item as ModbusData;
                            for (int i = 0; i < modItem.Data.Count; i++) {
                                result += modItem.Data[i].ToString();
                            }

                            logger.LogInformation($"Publish complete on {pipeName}: {modItem}");
                        }
                        else
                        {
                            logger.LogInformation($"Publish complete on {pipeName}: {item}");
                        }

                        // Should not have a delay here, since there is no reason to delay our dequeue action. If the queue as items, spit them out ASAP! Prevents queue buildup.
                        // await Task.Delay(config.ScanRate, stoppingToken);
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
                    if (fEInitState.IsInitialized) {
                        fEInitState.IsInitialized = false; // stream dropped, which is the best indication that we need to re-initialize
                        logger.LogWarning("Frontend Initialization State set to False!");
                    }
                }

                // Should not have a delay here, since there is no reason to delay our publish action. If the queue as items, spit them out ASAP! Prevents queue buildup.
                // await Task.Delay(config.ScanRate, stoppingToken);
            }

            logger.LogInformation(
                "Stopping stream publisher worker for {Type}",
                typeof(T).Name);
        }
    }
}
