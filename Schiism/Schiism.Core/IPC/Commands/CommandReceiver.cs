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
    public class CommandReceiver(string pipeName, INamedPipeFactory pipeFactory, PipeSerializer serializer, ILogger<CommandReceiver> logger)
    {
        /// <inheritdoc/>
        public async Task ReceiveAsync(Func<SettingsConfig, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation(
                        "Creating named pipe for {PipeName}",
                        pipeName);

                    using var pipe = pipeFactory.CreateServer(pipeName);

                    logger.LogInformation(
                        "Waiting for sender connection on {PipeName}",
                        pipeName);

                    await pipe.WaitForConnectionAsync(ct);

                    logger.LogInformation(
                        "Sender connected to {PipeName}",
                        pipeName);
                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        SettingsConfig? cmd = await serializer.DeserializeAsync<SettingsConfig>(pipe, ct);

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
                    logger.LogError(ex, "Command server error");
                }
            }
        }
    }
}
