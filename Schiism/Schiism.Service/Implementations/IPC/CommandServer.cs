namespace Schiism.Service.Implementations.IPC
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.IPC;
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading.Tasks;

    public class CommandServer<T>(string pipeName, ILogger<CommandServer<T>> logger) : ICommandServer<T>
    {
        private PipeSerializer Serializer => new();

        public async Task HandleClient(Func<T, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation(
                        "Creating named pipe for {PipeName}",
                        pipeName);

                    using var pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    logger.LogInformation(
                        "Waiting for client connection on {PipeName}",
                        pipeName);

                    await pipe.WaitForConnectionAsync(ct);

                    logger.LogInformation(
                        "Client connected to {PipeName}",
                        pipeName);

                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        var cmd = await Serializer.DeserializeAsync<T>(pipe, ct);

                        logger.LogInformation("Received command: {Command}", cmd);

                        await handler(cmd);
                    }
                }
                catch (EndOfStreamException)
                {
                    logger.LogInformation("Client disconnected from {PipeName}", pipeName);
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
