// <copyright file="ServiceCommandSender.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using System.IO.Pipes;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// Backend Command Sender implementation.
    /// </summary>
    /// <param name="pipeName">Name of the pipe that the command will be received from.</param>
    /// <param name="logger">Logger object for logging data to text file.</param>
    public class ServiceCommandSender(string pipeName, ILogger<ServiceCommandSender> logger) : ICommandSender
    {
        private readonly PipeSerializer Serializer = new();

        /// <inheritdoc/>
        public async Task SendAsync(SettingsConfig command, CancellationToken ct)
        {
            using NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            logger.LogInformation("Connecting to {0}", pipeName);
            await pipe.ConnectAsync(ct);
            logger.LogInformation("Connected to {0}", pipeName);

            await this.Serializer.SerializeAsync(pipe, command, ct);

            await pipe.FlushAsync(ct);
            logger.LogInformation("Command: {0} sent to {1}", command, pipeName);
        }
    }
}
