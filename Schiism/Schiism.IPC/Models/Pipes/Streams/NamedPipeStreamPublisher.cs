using Schiism.Core.Abstractions.IPC;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.IPC.Models.Pipes.Streams
{
    public class NamedPipeStreamPublisher<T>(string pipeName) : IStreamPublisher<T>
    {
        private NamedPipeServerStream? pipe;

        public async Task StartAsync(CancellationToken ct)
        {
            pipe = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(ct);
        }

        public async Task PublishAsync(T data, CancellationToken ct)
        {
            if (pipe?.IsConnected != true) return;

            await PipeSerializer.SerializeAsync(pipe, data, ct);
            await pipe.FlushAsync(ct);
        }
    }
}
