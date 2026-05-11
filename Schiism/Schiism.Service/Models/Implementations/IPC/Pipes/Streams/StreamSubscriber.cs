using Schiism.Core;
using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Abstractions.Modbus;
using System.IO.Pipes;

namespace Schiism.Service.Models.Implementations.IPC.Pipes.Streams
{
    public class StreamSubscriber<T>(string pipeName, ILogger<StreamSubscriber<T>> logger) : IStreamSubscriber<T>
    {
        private PipeSerializer Serializer => new();

        public async Task SubscribeAsync(Func<T, Task> onData, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var pipe = new NamedPipeClientStream(
                        ".",
                        pipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    // logger.LogInformation("Waiting for server connection on {PipeName}", pipeName);
                    await pipe.ConnectAsync(ct);
                    // logger.LogInformation("Client connected to {PipeName}", pipeName);

                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        // logger.LogInformation("Waiting for diagnostics message");
                        var data = await this.Serializer.DeserializeAsync<T>(pipe, ct);
                        await onData(data);
                        // logger.LogInformation("Received data on {PipeName}: {Data}", pipeName, data);
                    }
                }
                catch (OperationCanceledException)
                {
                    // logger.LogInformation("Subscription to {PipeName} cancelled", pipeName);
                    break;
                }
                catch (Exception ex)
                {
                    // logger.LogError(ex, $"Diagnostics subscriber failure: {ex}");
                    await Task.Delay(1000, ct);
                }
            }
        }
    }
}