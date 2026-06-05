// <copyright file="ServiceCommandSender.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Commands
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.Serialization;
    using System.IO.Pipes;

    /// <summary>
    /// Backend Command Sender implementation.
    /// </summary>
    /// <param name="pipeName">Name of the pipe that the command will be received from.</param>
    /// <param name="logger">Logger object for logging data to text file.</param>
    public class CommandSender(string pipeName, INamedPipeFactory pipeFactory, PipeSerializer serializer, ILogger<CommandSender> logger)
    {
        /// <inheritdoc/>
        public async Task SendAsync(SettingsConfig command, CancellationToken ct)
        {
            using var pipe = pipeFactory.CreateClient(pipeName);

            try
            {
            logger.LogInformation("Connecting to {0}", pipeName);
            await pipe.ConnectAsync(ct);
            logger.LogInformation("Connected to {0}", pipeName);

            await serializer.SerializeAsync(pipe, command, ct);

            await pipe.FlushAsync(ct);
            logger.LogInformation("Command: {0} sent to {1}", command, pipeName);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to send command {ex}");
                throw;
            }
        }
    }
}
