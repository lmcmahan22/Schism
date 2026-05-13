using System.IO.Pipes;

namespace Schiism.Service.Implementations.IPC
{
    using Schiism.Core.Abstractions.IPC.Commands;
    using Schiism.Core.Models.IPC;

    /// <summary>
    /// connect → send → disconnect. Commands don't need to stay connected the whole time, it just takes up bandwidth and resources on the app.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pipeName"></param>
    /// <param name="logger"></param>
    public class ServiceCommandSender<T>(string pipeName, ILogger<ServiceCommandSender<T>> logger) : ICommandSender<T>
    {
        private PipeSerializer Serializer => new();

        public async Task SendAsync(T command, Func<T, Task> handler, CancellationToken ct)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            logger.LogInformation("Connecting to {0}", pipeName);

            await pipe.ConnectAsync(ct);

            logger.LogInformation("Connected to {0}", pipeName);
            await Serializer.SerializeAsync(pipe, command, ct);

            logger.LogInformation("Command: {0} sent to {1}", command, pipeName);

            await pipe.FlushAsync(ct);
            await handler(command);
        }
    }
}
