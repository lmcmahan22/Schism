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
    public class ServiceStreamPublisherWorker<T>(string pipeName, IConfigState config, IInitializedState fEInitState, IStreamQueue<T> queue, IStreamPublisher<T> publisher, ILogger<ServiceStreamPublisherWorker<T>> logger) : BackgroundService
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

                // Create the pipe
                NamedPipeServerStream? pipe = null;
                pipe = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.Out,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // Try to connect on the pipe, return to the top of the loop if it fails
                try
                {
                    logger.LogInformation($"Waiting for client on {pipeName}");
                    await pipe.WaitForConnectionAsync(stoppingToken);

                    logger.LogInformation($"Client connected on {pipeName}");

                    while (!stoppingToken.IsCancellationRequested && pipe.IsConnected)
                    {
                        if (queue.Count == 0)
                        {
                            await Task.Delay(100, stoppingToken);
                            continue;
                        }

                        T? item = await queue.DequeueAsync(stoppingToken);

                        try
                        {
                            logger.LogInformation($"Publishing on {pipeName}");
                            await publisher.PublishAsync(pipe, item, stoppingToken);

                            if (item.GetType() == typeof(ModbusData))
                            {
                                string result = string.Empty;
                                ModbusData? modItem = item as ModbusData;
                                for (int i = 0; i < modItem.Data.Count; i++)
                                {
                                    result += modItem.Data[i].ToString();
                                }

                                logger.LogInformation($"Publish complete on {pipeName}: {modItem}");
                                logger.LogInformation($"Modbus Data: {result}");
                            }
                            else
                            {
                                logger.LogInformation($"Publish complete on {pipeName}: {item}");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (IOException ioEx)
                        {
                            logger.LogWarning(ioEx, "Pipe broke during publish");
                            fEInitState.IsInitialized = false;
                            logger.LogWarning("Frontend Initialization State set to False!");
                            break; // reconnect
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Non-fatal publish error. Continuing...");
                            continue; // skip item only
                        }

                        // Should not have a delay here, since there is no reason to delay our dequeue action. If the queue as items, spit them out ASAP! Prevents queue buildup.
                        // await Task.Delay(config.ScanRate, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException ioEx)
                {
                    logger.LogWarning(ioEx, "Pipe IO failure - retrying connection");
                    continue;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected connection failure");
                    await Task.Delay(1000, stoppingToken);
                }
                finally
                {
                    pipe.Dispose();
                }

                // Should not have a delay here, since there is no reason to delay our publish action. If the queue as items, spit them out ASAP! Prevents queue buildup.
                // await Task.Delay(config.ScanRate, stoppingToken);
            }
        }
    }
}
