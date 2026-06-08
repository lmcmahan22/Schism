// <copyright file="StreamPublisherWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.HostedServices
{
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.IPC.Streams;
    using System.IO.Pipes;

    /// <summary>
    /// Publishing Worker. Both ModbusData and ConnectionDiagnostics are published through this same worker type, just seperate instances.
    /// </summary>
    /// <typeparam name="T">The type of the stream item, either ModbusData or ConnectionDiagnostics.</typeparam>
    public class StreamPublisherWorker<T>(
        string pipeName,
        INamedPipeFactory pipeFactory,
        ConfigState config,
        InitStatus initStatus,
        StreamQueue<T> queue,
        StreamPublisher<T> publisher,
        ILogger<StreamPublisherWorker<T>> logger,
        IHostApplicationLifetime lifetime)
        : BackgroundService
    {
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Don't run this worker until the application is fully started (i.e. all three Worker's StartAsync() methods are complete).
            await Task.Run(
                () => lifetime.ApplicationStarted.WaitHandle.WaitOne(), stoppingToken);

            logger.LogInformation($"Service Stream Publisher Worker for {pipeName} has started");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!initStatus.IsInitialized)
                {
                    // Wait a second for the WPF app to initialize
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                // The difference between Streams and Commands is who owns the pipes.
                // The Worker needs to loop around the stream, so stream pipes are owned by the worker.
                // Commands are one time sends, so those are owned by the Command Send and Receive classes (currently).
                using var pipe = pipeFactory.CreateNPServer(pipeName);

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
                        else
                        {
                            logger.LogInformation($"{typeof(T).Name} Queue Count:{queue.Count}");
                        }

                        T? item = await queue.DequeueAsync(stoppingToken);

                        try
                        {
                            logger.LogInformation($"Publishing on {pipeName}");

                            await publisher.PublishAsync(pipe, item, stoppingToken);

                            //if (item.GetType() == typeof(ModbusDataDTO))
                            //{
                            //    string result = string.Empty;
                            //    ModbusDataDTO? modItem = item as ModbusDataDTO;
                            //    for (int i = 0; i < modItem.Data.Count; i++)
                            //    {
                            //        result += modItem.Data[i].ToString();
                            //    }

                            //    logger.LogInformation($"Publish complete on {pipeName}: {modItem}");
                            //    logger.LogInformation($"Modbus Data: {result}");
                            //}
                            //else
                            //{
                            //    logger.LogInformation($"Publish complete on {pipeName}: {item}");
                            //}
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (IOException ioEx)
                        {
                            logger.LogWarning(ioEx, "Pipe broke during publish");
                            initStatus.IsInitialized = false;
                            logger.LogWarning("Frontend Initialization State set to False!");
                            break; // reconnect
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Non-fatal publish error. Continuing...");
                            continue; // skip item only
                        }
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
            }
        }
    }
}
