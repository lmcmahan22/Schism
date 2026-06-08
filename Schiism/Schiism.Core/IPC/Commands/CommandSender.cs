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
        public async Task SendAsync(SettingsConfigDTO command, CancellationToken ct)
        {
            try
            {
                logger.LogInformation(
                   "Creating named pipe for {PipeName}",
                   pipeName);

                using var pipe = pipeFactory.CreateNPServer(pipeName);

                logger.LogInformation(
                   "Waiting for sender connection on {PipeName}",
                   pipeName);

                await pipe.WaitForConnectionAsync(ct);

                logger.LogInformation(
                   "Sender connected to {PipeName}",
                   pipeName);

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
