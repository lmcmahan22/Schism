// <copyright file="FECommandSender.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.IPC
{
    using System;
    using System.IO.Pipes;
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.WPF.Models.Implementations.States;

    /// <summary>
    /// Frontend Command Sender implementation.
    /// </summary>
    /// <param name="pipeName"> Name of pipe that the command data should be sent along.</param>
    public class WPFCommandSender : ICommandSender
    {
        private readonly PipeSerializer Serializer = new();
        private readonly string pipeName;
        private readonly ILogger logger;

        public WPFCommandSender(
            string pipeName,
            ILoggerFactory factory)
        {
            this.pipeName = pipeName;
            this.logger = factory.CreateLogger<WPFCommandSender>();
        }

        /// <inheritdoc/>
        public async Task SendAsync(SettingsConfig command, CancellationToken ct)
        {
            using NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            try
            {
                logger.LogInformation("Connecting to {0}", pipeName);
                await pipe.ConnectAsync(ct);
                logger.LogInformation("Connected to {0}", pipeName);

                await this.Serializer.SerializeAsync(pipe, command, ct);

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
