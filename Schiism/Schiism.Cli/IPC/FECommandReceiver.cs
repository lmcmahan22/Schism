namespace Schiism.Cli.IPC
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.IPC;
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading.Tasks;

    public class FECommandReceiver<T>(string pipeName) : ICommandReceiver<T>
    {
        private PipeSerializer Serializer => new();

        public async Task ReceiveAsync(Func<T, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"Creating named pipe for {pipeName}");

                    using var pipe = new NamedPipeServerStream(
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
                        var cmd = await Serializer.DeserializeAsync<T>(pipe, ct);

                        Console.WriteLine($"Received command: {cmd}");

                        await handler(cmd);

                        return; // Single reciept complete! Get out of here!
                    }
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine($"Client disconnected from {pipeName}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Command server shutting down for {pipeName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in command server for {pipeName}: {ex.Message}");
                }
            }
        }
    }
}
