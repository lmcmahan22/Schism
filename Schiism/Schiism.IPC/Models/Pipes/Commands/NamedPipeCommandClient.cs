using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.IPC.Models.Pipes.Commands
{
    using Schiism.Core.Abstractions.IPC;
    using Schiism.IPC.Models;

    public class NamedPipeCommandClient<T>(string pipeName) : ICommandClient<T>
    {
        public async Task SendAsync(T command, CancellationToken ct)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            await pipe.ConnectAsync(ct);

            await PipeSerializer.SerializeAsync(pipe, command, ct);
        }
    }
}
