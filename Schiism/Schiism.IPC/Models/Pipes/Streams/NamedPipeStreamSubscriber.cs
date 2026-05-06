using Schiism.Core.Abstractions.IPC.Streams;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.IPC.Models.Pipes.Streams
{
    public class NamedPipeStreamSubscriber<T>(string pipeName) : IStreamSubscriber<T>
    {
        public async Task StartAsync(Func<T, Task> onData, CancellationToken ct)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.In);
            await pipe.ConnectAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                var data = await PipeSerializer.DeserializeAsync<T>(pipe, ct);
                await onData(data);
            }
        }
    }
}
