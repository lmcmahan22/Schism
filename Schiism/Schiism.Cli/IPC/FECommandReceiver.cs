// <copyright file="FECommandReceiver.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Cli.IPC
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading.Tasks;
    using Schiism.Core.IPC;
    using Schiism.Core.IPC.Commands;
    using Schiism.Core.IPC.DTOs.Commands;

    /// <summary>
    /// Command Receiver implementation for the front end.
    /// </summary>
    /// <param name="pipeName">Name of pipe that the command will be received from.</param>
    public class FECommandReceiver(string pipeName) : ICommandReceiver
    {
        private PipeSerializer Serializer => new();

        /// <inheritdoc/>
        public async Task ReceiveAsync(Func<SettingsConfig, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"Creating named pipe for {pipeName}");

                    using NamedPipeServerStream pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    Console.WriteLine($"Waiting for sender connection on {pipeName}");

                    await pipe.WaitForConnectionAsync(ct);

                    Console.WriteLine($"Sender connected to {pipeName}");
                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        SettingsConfig? cmd = await this.Serializer.DeserializeAsync<SettingsConfig>(pipe, ct);

                        if (cmd is null)
                        {
                            Console.WriteLine("Received null command, ignoring");
                            continue;
                        }

                        Console.WriteLine($"Received command: {cmd}");
                        await handler(cmd);
                        return;
                    }
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine("Sender disconnected from {PipeName}", pipeName);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Command server shutting down");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command server error: {ex}");
                }
            }
        }
    }
}
