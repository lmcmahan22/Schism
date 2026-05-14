using Microsoft.Extensions.Logging;
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

                NamedPipeClientStream? pipe = null;

                try
                {
                    pipe = new NamedPipeClientStream(
                        ".",
                        pipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    // Console.WriteLine($"Attempting connection to {pipeName}");
                    
                    await pipe.ConnectAsync(ct);
                    
                    // Console.WriteLine($"Connected to {pipeName}");

                    var data = await Serializer.DeserializeAsync<T>(pipe, ct);
                    try
                    {
                        await onData(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Subscriber callback failed for {PipeName}: {ex}",
                            pipeName, ex);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Subscription to {pipeName} cancelled");
                    break;
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine($"Subscription to {pipeName} dropped unexpectedly");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unknown error in subscription to {pipeName}: {ex}");
                    throw;
                }
                finally
                {
                    pipe?.Dispose();
                }
            }
        }
    }
}