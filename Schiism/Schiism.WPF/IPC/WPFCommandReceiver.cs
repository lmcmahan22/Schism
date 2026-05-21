// <copyright file="FECommandReceiver.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.IPC
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// Command Receiver implementation for the front end.
    /// </summary>
    /// <param name="pipeName">Name of pipe that the command will be received from.</param>
    public class WPFCommandReceiver : ICommandReceiver
    {
        private readonly PipeSerializer Serializer = new();
        private readonly string pipeName;
        private readonly ILogger<WPFCommandReceiver> logger;

        public WPFCommandReceiver(string pipeName, ILoggerFactory factory)
        {
            this.pipeName = pipeName;
            this.logger = factory.CreateLogger<WPFCommandReceiver>();
        }

        /// <inheritdoc/>
        public async Task ReceiveAsync(Func<SettingsConfig, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation($"Creating named pipe for {pipeName}");

                    using NamedPipeServerStream pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    logger.LogInformation($"Waiting for sender connection on {pipeName}");

                    await pipe.WaitForConnectionAsync(ct);

                    logger.LogInformation($"Sender connected to {pipeName}");
                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        SettingsConfig? cmd = await this.Serializer.DeserializeAsync<SettingsConfig>(pipe, ct);

                        if (cmd is null)
                        {
                            logger.LogInformation("Received null command, ignoring");
                            continue;
                        }

                        logger.LogInformation($"Received command: {cmd}");
                        await handler(cmd);
                        return;
                    }
                }
                catch (EndOfStreamException)
                {
                    logger.LogError("Sender disconnected from {PipeName}", pipeName);
                }
                catch (OperationCanceledException)
                {
                    logger.LogError("Command server shutting down");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Command server error: {ex}");
                }
            }
        }
    }
}
