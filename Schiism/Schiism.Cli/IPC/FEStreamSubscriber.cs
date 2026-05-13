using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.IPC;
using System.IO.Pipes;

namespace Schiism.Cli.IPC
{
    public class FEStreamSubscriber<T>(string pipeName) : IStreamSubscriber<T>
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

                    // Console.WriteLine($"Attempting connection to {pipeName}");
                    
                    await pipe.ConnectAsync(ct);
                    
                    // Console.WriteLine($"Connected to {pipeName}");

                    while (pipe.IsConnected && !ct.IsCancellationRequested)
                    {
                        var data = await Serializer.DeserializeAsync<T>(pipe, ct);
                        await onData(data);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Subscription to {pipeName} cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in subscription to {pipeName}: {ex}");
                    await Task.Delay(1000, ct);
                }
            }
        }
    }
}