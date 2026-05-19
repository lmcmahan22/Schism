using Microsoft.Extensions.Hosting;
using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.Core.Models.IPC.DTOs.Streams;

namespace Schiism.WPF.IPC.Workers
{
    public class ModbusSubscriberWorker(IStreamSubscriber<ModbusData> subscriber, IStreamDataState<ModbusData> dataState) : BackgroundService
    {
        // should this be a collection of ModbusData polls, just so we don't lose any data s we're trying to print it?
        private ModbusData rawData;

        public ModbusData RawData { get => rawData; }

        // Handle connection loss???
        protected override async Task ExecuteAsync(CancellationToken cts)
        {
            // "Starting stream subscriber worker for Modbus Data"

            while (!cts.IsCancellationRequested)
            {
                await subscriber.SubscribeAsync(HandleModbusAsync, cts);
            }
        }

        private Task HandleModbusAsync(ModbusData msg)
        {
            dataState.Update(msg);

            return Task.CompletedTask;
        }
    }
}
