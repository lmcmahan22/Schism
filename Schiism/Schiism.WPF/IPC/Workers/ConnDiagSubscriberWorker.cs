using Microsoft.Extensions.Hosting;
using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.IPC.DTOs.Streams;

namespace Schiism.WPF.IPC.Workers
{
    public class ConnDiagSubscriberWorker(IStreamSubscriber<ConnectionDiagnostics> subscriber, IStreamDataState<ConnectionDiagnostics> diagState) : BackgroundService
    {
        // Handle connection loss???
        protected override async Task ExecuteAsync(CancellationToken cts)
        {
            // "Starting stream subscriber worker for Modbus Data"

            while (!cts.IsCancellationRequested)
            {
                await subscriber.SubscribeAsync(HandleConnDiagAsync, cts);
            }
        }

        private Task HandleConnDiagAsync(ConnectionDiagnostics msg)
        {
            diagState.Update(msg);

            return Task.CompletedTask;
        }
    }
}
