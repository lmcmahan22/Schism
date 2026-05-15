// <copyright file="FECommandSender.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Cli.IPC
{
    using System;
    using System.IO.Pipes;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Models.IPC;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// Frontend Command Sender implementation.
    /// </summary>
    /// <param name="pipeName"> Name of pipe that the command data should be sent along.</param>
    public class FECommandSender(string pipeName) : ICommandSender
    {
        private PipeSerializer Serializer => new();

        /// <inheritdoc/>
        public async Task SendAsync(SettingsConfig command, CancellationToken ct)
        {
            using NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            Console.WriteLine("Connecting to {0}", pipeName);
            await pipe.ConnectAsync(ct);
            Console.WriteLine("Connected to {0}", pipeName);

            await this.Serializer.SerializeAsync(pipe, command, ct);

            await pipe.FlushAsync(ct);
            Console.WriteLine("Command: {0} sent to {1}", command, pipeName);
        }
    }
}
