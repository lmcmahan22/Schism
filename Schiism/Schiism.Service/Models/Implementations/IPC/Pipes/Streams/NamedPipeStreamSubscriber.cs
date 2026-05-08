using Schiism.Core;
using Schiism.Core.Abstractions.IPC.Streams;
using System.IO.Pipes;

namespace Schiism.Service.Models.Implementations.IPC.Pipes.Streams
{
    public class NamedPipeStreamSubscriber<T>(string pipeName, ILogger<NamedPipeStreamSubscriber<T>> logger) : IStreamSubscriber<T>
    {
        private PipeSerializer Serializer => new();

        public async Task StartAsync(Func<T, Task> onData, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                logger.LogInformation("Creating named pipe for {PipeName}", pipeName);

                var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In);

                logger.LogInformation("Waiting for server connection on {PipeName}", pipeName);

                await pipe.ConnectAsync(ct);

                logger.LogInformation("Client connected to {PipeName}", pipeName);

                var data = await Serializer.DeserializeAsync<T>(pipe, ct);
                await onData(data);
            }
        }
    }
}
