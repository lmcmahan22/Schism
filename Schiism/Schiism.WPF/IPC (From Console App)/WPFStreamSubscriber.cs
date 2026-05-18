// <copyright file="FEStreamSubscriber.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.IPC
{
    using System.IO.Pipes;
    using Schiism.Core.Abstractions.IPC.States;
    // using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC;

    /// <summary>
    /// Stream subscriber implementation for the front end.
    /// </summary>
    /// <typeparam name="T">Defines the object type that will be expected on this stream.</typeparam>
    /// <param name="pipeName">Name of pipe that the stream data will be received from.</param>
    public class WPFStreamSubscriber<T>(string pipeName, IStreamDataState<T> dataState) : IStreamSubscriber<T>
    {
        private PipeSerializer Serializer => new();

        /// <inheritdoc/>
        public async Task SubscribeAsync(Func<T, Task> onData, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeClientStream? pipe = null;

                try
                {
                    pipe = new NamedPipeClientStream(
                        ".",
                        pipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    await pipe.ConnectAsync(ct);

                    T? data = await this.Serializer.DeserializeAsync<T>(pipe, ct);

                    if (data == null)
                    {
                        Console.WriteLine($"Received null data from {pipeName}");
                        continue;
                    }

                    try
                    {
                        await onData(data);
                        dataState.Update(data);
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

                // catch (EndOfStreamException)
                // {
                //    Console.WriteLine($"Subscription to {pipeName} dropped unexpectedly");
                //    throw;
                // }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unknown error in subscription to {pipeName}: {ex}");
                    throw;
                }
                finally
                {
                    pipe?.Dispose();
                }
            }
        }
    }
}