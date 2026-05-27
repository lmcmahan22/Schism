// <copyright file="FEStreamSubscriber.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Cli.IPC
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC;
    using System.IO.Pipes;

    /// <summary>
    /// Stream subscriber implementation for the front end.
    /// </summary>
    /// <typeparam name="T">Defines the object type that will be expected on this stream.</typeparam>
    /// <param name="pipeName">Name of pipe that the stream data will be received from.</param>
    public class FEStreamSubscriber<T>(string pipeName) : IStreamSubscriber<T>
    {
        private PipeSerializer Serializer => new();

        /// <inheritdoc/>
        public async Task<T?> SubscribeAsync(PipeStream pipe, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeClientStream? clientPipe = null;

                try
                {
                    clientPipe = new NamedPipeClientStream(
                        ".",
                        pipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    await clientPipe.ConnectAsync(ct);

                    T? data = await this.Serializer.DeserializeAsync<T>(clientPipe, ct);

                    if (data == null)
                    {
                        Console.WriteLine($"Received null data from {pipeName}");
                        continue;
                    }

                    try
                    {
                        Console.WriteLine($"Received data on {typeof(T).Name} pipe: {data}.");
                        return data;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Subscriber callback failed for {PipeName}: {ex}",
                            pipeName,
                            ex);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Subscription to {pipeName} cancelled");
                    break;
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine($"Subscription to {pipeName} dropped unexpectedly");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unknown error in subscription to {pipeName}: {ex}");
                    throw;
                }
                finally
                {
                    clientPipe?.Dispose();
                }
            }
        }
    }
}