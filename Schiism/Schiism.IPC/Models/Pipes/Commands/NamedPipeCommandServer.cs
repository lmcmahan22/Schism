namespace Schiism.IPC.Models.Pipes.Commands
{
    using Schiism.Core.Abstractions.IPC;
    using Schiism.IPC.Models;
    using System;
    using System.IO.Pipes;
    using System.Threading.Tasks;

    public class NamedPipeCommandServer<T>(string pipeName) : ICommandServer<T>
    {
        public async Task StartAsync(Func<T, Task> handler, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances);

                await pipe.WaitForConnectionAsync(ct);

                _ = Task.Run(
                    async () =>
                {
                    using (pipe)
                    {
                        var cmd = await PipeSerializer.DeserializeAsync<T>(pipe, ct);
                        await handler(cmd);
                    }
                }, ct);
            }
        }
    }
}
