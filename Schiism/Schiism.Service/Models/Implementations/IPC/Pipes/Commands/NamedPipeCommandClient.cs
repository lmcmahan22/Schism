using System.IO.Pipes;

namespace Schiism.Service.Models.Implementations.IPC.Pipes.Commands
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core;
    using Schiism.Core.Abstractions.IPC.Commands;

    public class NamedPipeCommandClient<T>(string pipeName, ILogger<NamedPipeCommandClient<T>> logger) : ICommandClient<T>
    {
        private PipeSerializer Serializer => new();

        public async Task SendAsync(T command, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                logger.LogInformation("Creating named pipe for {PipeName}", pipeName);

                var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);

                logger.LogInformation("Waiting for server connection on {PipeName}", pipeName);

                await pipe.ConnectAsync(ct);

                logger.LogInformation("Client connected to {PipeName}", pipeName);

                await this.Serializer.SerializeAsync(pipe, command, ct);
            }
        }
    }
}
