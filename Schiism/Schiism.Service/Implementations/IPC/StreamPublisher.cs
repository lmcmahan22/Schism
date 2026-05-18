// <copyright file="StreamPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using System.IO.Pipes;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC;

    /// <summary>
    /// Implementing class for the IStreamPublisher interface.
    /// </summary>
    /// <typeparam name="T">The object type sent along the stream.</typeparam>
    /// <param name="pipeName">Pipe name, DI'd.</param>
    /// <param name="fEInitState">Frontend Initialized state object, DI'd.</param>
    /// <param name="logger">File Logger object, DI'd.</param>
    public class StreamPublisher<T>(string pipeName, ILoadConfigState fEInitState, ILogger<StreamPublisher<T>> logger) : IStreamPublisher<T>
    {
        private readonly List<NamedPipeServerStream> clients = [];
        private readonly PipeSerializer serializer = new();
        private bool isConnected;

        /// <inheritdoc/>
        public bool IsConnected => this.isConnected;

        /// <summary>
        /// Triggers the looping AcceptLoopAsync method.
        /// Loop completes once a pipe connection is made.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The asynchronous connection task after it has begun.</returns>
        public Task StartAsync(CancellationToken ct)
        {
            _ = Task.Run(() => this.AcceptLoopAsync(ct), ct);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task PublishAsync(T data, CancellationToken ct)
        {
            if (this.clients.Count == 0)
            {
                return;
            }

            List<NamedPipeServerStream> disconnectedClients = new List<NamedPipeServerStream>();
            foreach (NamedPipeServerStream client in this.clients)
            {
                try
                {
                    await this.serializer.SerializeAsync(client, data, ct);
                    await client.FlushAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogInformation(
                        ex,
                        "Removing disconnected client from {PipeName}",
                        pipeName);
                    disconnectedClients.Add(client);
                }
            }

            foreach (NamedPipeServerStream client in disconnectedClients)
            {
                client.Dispose();
                this.clients.Remove(client);
                this.isConnected = false;
                if (fEInitState.IsInitialized)
                {
                    fEInitState.IsInitialized = false; // This works for just one client connected to the stream publisher, but your future-considerate` one to many layout here goes against this implementation.
                    logger.LogInformation("Frontend Initialization State set to False!");
                }
            }
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Creating named pipe for {PipeName}", pipeName);

                    NamedPipeServerStream client = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.Out,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    logger.LogInformation(
                        "Waiting for subscriber on {PipeName}", pipeName);

                    await client.WaitForConnectionAsync(ct);

                    logger.LogInformation(
                        "Subscriber connected on {PipeName}",
                        pipeName);

                    this.isConnected = true;

                    this.clients.Add(client);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Pipe accept loop error for {PipeName}",
                        pipeName);
                }
            }
        }
    }
}
