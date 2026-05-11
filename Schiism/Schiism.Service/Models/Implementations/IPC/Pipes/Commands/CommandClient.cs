using System.IO.Pipes;

namespace Schiism.Service.Models.Implementations.IPC.Pipes.Commands
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core;
    using Schiism.Core.Abstractions.IPC.Commands;

    /// <summary>
    /// connect → send → disconnect. Commands don't need to stay connected the whole time, it just takes up bandwidth and resources on the app.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pipeName"></param>
    /// <param name="logger"></param>
    public class CommandClient<T>(string pipeName, ILogger<CommandClient<T>> logger) : ICommandClient<T>
    {
        private PipeSerializer Serializer => new();

        public async Task SendAsync(T command, CancellationToken ct)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            logger.LogInformation("Connecting to {PipeName}", pipeName);

            await pipe.ConnectAsync(ct);

            logger.LogInformation("Connected to {PipeName}", pipeName);

            await this.Serializer.SerializeAsync(pipe, command, ct);

            await pipe.FlushAsync(ct);

            logger.LogInformation("Command sent to {PipeName}", pipeName);
        }
    }
}
