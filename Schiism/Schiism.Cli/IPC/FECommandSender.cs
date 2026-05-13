using System.IO.Pipes;

namespace Schiism.Cli.IPC
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Models.IPC;
    using System;

    /// <summary>
    /// connect → send → disconnect. Commands don't need to stay connected the whole time, it just takes up bandwidth and resources on the app.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pipeName"></param>
    /// <param name="logger"></param>
    public class FECommandSender<T>(string pipeName) : ICommandSender<T>
    {
        private PipeSerializer Serializer => new();

        // Handler is not used in this implementation, but it's part of the interface contract. It can be used for logging or other side effects if needed.
        public async Task SendAsync(T command, Func<T, Task> handler, CancellationToken ct)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            Console.WriteLine("Connecting to {0}", pipeName);

            await pipe.ConnectAsync(ct);

            Console.WriteLine("Connected to {0}", pipeName);

            await Serializer.SerializeAsync(pipe, command, ct);

            Console.WriteLine("Command: {0} sent to {1}", command, pipeName);

            await pipe.FlushAsync(ct);
        }
    }
}
