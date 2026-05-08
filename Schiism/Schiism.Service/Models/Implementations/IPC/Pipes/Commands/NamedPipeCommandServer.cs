namespace Schiism.Service.Models.Implementations.IPC.Pipes.Commands
{
    using Schiism.Core;
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Service.Models.Implementations.IPC.Pipes.Streams;
    using System;
    using System.IO.Pipes;
    using System.Threading.Tasks;

    public class NamedPipeCommandServer<T>(string pipeName, ILogger<NamedPipeCommandServer<T>> logger) : ICommandServer<T>
    {
        private PipeSerializer Serializer => new();

        public async Task StartAsync(Func<T, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                logger.LogInformation("Creating named pipe for {PipeName}", pipeName);

                var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances);

                logger.LogInformation("Waiting for client connection on {PipeName}", pipeName);

                await pipe.WaitForConnectionAsync(ct);

                logger.LogInformation("Client connected to {PipeName}", pipeName);

                _ = Task.Run(
                    async () =>
                {
                    using (pipe)
                    {
                        var cmd = await Serializer.DeserializeAsync<T>(pipe, ct);
                        await handler(cmd);
                    }
                }, ct);
            }
        }
    }
}
