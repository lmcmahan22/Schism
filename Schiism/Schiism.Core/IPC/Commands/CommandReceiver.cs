// <copyright file="ServiceCommandReceiver.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Commands
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.Serialization;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Backend Command Receiver implementation.
    /// </summary>
    /// <param name="pipeName">Name of the pipe that the command will be received from.</param>
    /// <param name="logger">Logger object for logging data to text file.</param>
    public class CommandReceiver<T>(string pipeName, INamedPipeFactory pipeFactory, PipeSerializer serializer, ILogger<CommandReceiver<T>> logger)
    {
        /// <inheritdoc/>
        public async Task ReceiveAsync(Func<T, Task> handler, CancellationToken ct)
        {
                try
                {
                    logger.LogInformation(
                        "Connecting to {PipeName}",
                        pipeName);

                    using var pipe = pipeFactory.CreateNPClient(pipeName);

                    await pipe.ConnectAsync(ct);

                    logger.LogInformation(
                        "Connected to {PipeName}",
                        pipeName);

                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        T? cmd = await serializer.DeserializeAsync<T>(pipe, ct);

                        if (cmd is null)
                        {
                            logger.LogWarning("Received null command, ignoring");
                            continue;
                        }

                        logger.LogInformation("Received command: {Command}", cmd);
                        await handler(cmd);
                        return; // Single reciept complete! Get out of here!
                    }
                }
                catch (EndOfStreamException)
                {
                    logger.LogInformation("Sender disconnected from {PipeName}", pipeName);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Command server shutting down");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Command server error {ex}");
                }
        }
    }
}
