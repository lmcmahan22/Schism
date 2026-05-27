// <copyright file="ServiceCommandReceiver.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Security.AccessControl;
    using System.Threading.Tasks;

    /// <summary>
    /// Backend Command Receiver implementation.
    /// </summary>
    /// <param name="pipeName">Name of the pipe that the command will be received from.</param>
    /// <param name="logger">Logger object for logging data to text file.</param>
    public class ServiceCommandReceiver(string pipeName, ILogger<ServiceCommandReceiver> logger) : ICommandReceiver
    {
        private readonly PipeSerializer Serializer = new();

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

                    var security = new PipeSecurity();

                    security.AddAccessRule(new PipeAccessRule(
                        "Everyone",
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));

                    using NamedPipeServerStream pipe = NamedPipeServerStreamAcl.Create(
                        pipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        0,
                        0,
                        security);

                    logger.LogInformation(
                        "Waiting for sender connection on {PipeName}",
                        pipeName);

                    await pipe.WaitForConnectionAsync(ct);

                    logger.LogInformation(
                        "Sender connected to {PipeName}",
                        pipeName);
                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        SettingsConfig? cmd = await this.Serializer.DeserializeAsync<SettingsConfig>(pipe, ct);

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
