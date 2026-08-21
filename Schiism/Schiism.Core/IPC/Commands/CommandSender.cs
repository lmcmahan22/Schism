// <copyright file="ServiceCommandSender.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Commands
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.PipeControl;
    using Schiism.Core.IPC.Serialization;

    /// <summary>
    /// Backend Command Sender implementation.
    /// </summary>
    /// <param name="pipeName">Name of the pipe that the command will be received from.</param>
    /// <param name="logger">Logger object for logging data to text file.</param>
    public class CommandSender<T>(string pipeName, INamedPipeFactory pipeFactory, PipeSerializer serializer, ILogger<CommandSender<T>> logger)
    {
        /// <inheritdoc/>
        public async Task SendAsync(T command, CancellationToken ct)
        {
            try
            {
                logger.LogInformation(
                   "[CORE] Creating named pipe for {PipeName}",
                   pipeName);

                using var pipe = pipeFactory.CreateNPServer(pipeName);

                logger.LogInformation(
                   "[CORE] Waiting for receiver connection on {PipeName}",
                   pipeName);

                await pipe.WaitForConnectionAsync(ct);

                logger.LogInformation(
                   "[CORE] Receiver connected to {PipeName}",
                   pipeName);

                await serializer.SerializeAsync(pipe, command, ct);

                await pipe.FlushAsync(ct);
                // logger.LogInformation("[CORE] Command: {0} sent on {1}", command, pipeName);
                logger.LogInformation("[CORE] {0} command sent on {1}", typeof(T).Name, pipeName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CORE] Failed to send {0} command on {PipeName}", typeof(T).Name, pipeName);
                throw;
            }
        }
    }
}
