using Schiism.Core;
using Schiism.Core.Abstractions.IPC.Streams;
using System.IO.Pipes;

namespace Schiism.Service.Models.Implementations.IPC.Pipes.Streams
{
    public class NamedPipeStreamPublisher<T>(string pipeName, ILogger<NamedPipeStreamPublisher<T>> logger) : IStreamPublisher<T>
    {
        private NamedPipeServerStream? pipe;

        private PipeSerializer Serializer => new();

        public async Task StartAsync(CancellationToken ct)
        {
            logger.LogInformation("Creating named pipe for {PipeName}", pipeName);

            pipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            logger.LogInformation("Waiting for client connection on {PipeName}", pipeName);

            await pipe.WaitForConnectionAsync(ct);

            logger.LogInformation("Client connected to {PipeName}", pipeName);
        }

        public async Task PublishAsync(T data, CancellationToken ct)
        {
            if (pipe?.IsConnected != true) return;

            await Serializer.SerializeAsync(pipe, data, ct);
            await pipe.FlushAsync(ct);
        }
    }
}
