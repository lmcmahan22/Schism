using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.IPC;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Schiism.Service.Implementations.IPC
{
    public class StreamPublisher<T>(string pipeName, ILogger<StreamPublisher<T>> logger) : IStreamPublisher<T>
    {
        private readonly List<NamedPipeServerStream> clients = [];
        private readonly PipeSerializer serializer = new();

        public Task StartAsync(CancellationToken ct)
        {
            _ = Task.Run(() => AcceptLoopAsync(ct), ct);
            return Task.CompletedTask;
        }

        public async Task PublishAsync(T data, CancellationToken ct)
        {
            if (clients.Count == 0)
            {
                return;
            }

            var disconnectedClients = new List<NamedPipeServerStream>();
            foreach (var client in clients)
            {
                try
                {
                    if (!client.IsConnected)
                    {
                        disconnectedClients.Add(client);
                        continue;
                    }

                    await serializer.SerializeAsync(client, data, ct);
                    await client.FlushAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Removing disconnected client from {PipeName}", pipeName);
                    disconnectedClients.Add(client);
                }
            }

            foreach (var client in disconnectedClients)
            {
                client.Dispose();
                clients.Remove(client);
            }
        }

        public async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    logger.LogInformation("Creating named pipe for {PipeName}", pipeName);

                    var client = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.Out,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    logger.LogInformation(
                        "Waiting for subscriber on {PipeName}", pipeName);

                    await client.WaitForConnectionAsync(ct);

                    logger.LogInformation(
                        "Subscriber connected on {PipeName}", pipeName);

                    clients.Add(client);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Pipe accept loop error for {PipeName}", pipeName);
                }
            }
        }
    }
}
