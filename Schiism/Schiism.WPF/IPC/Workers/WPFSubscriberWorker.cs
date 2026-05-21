using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schiism.Core.Abstractions.IPC.States;
using Schiism.Core.Abstractions.IPC.Streams;
using Schiism.WPF.Models.Implementations.States;
using System.IO;
using System.IO.Pipes;

namespace Schiism.WPF.IPC.Workers
{
    public class WPFSubscriberWorker<T> : BackgroundService
    {
        // No in-line constructor here, in order to support the logger:
        private readonly ILogger logger;
        private readonly IStreamSubscriber<T> subscriber;
        private readonly WPFStreamDataState<T> dataState;
        private WPFInitializedState initState;
        private readonly string pipeName;

        // should this be a collection of ModbusData polls, just so we don't lose any data s we're trying to print it?
        private T rawData;

        public T RawData { get => rawData; }

        public WPFSubscriberWorker(string pipeName, IStreamSubscriber<T> subscriber, WPFStreamDataState<T> dataState, WPFInitializedState initState, ILoggerFactory factory)
        {
            this.logger = factory.CreateLogger<WPFSubscriberWorker<T>>();
            this.subscriber = subscriber;
            this.dataState = dataState;
            this.initState = initState;
            this.pipeName = pipeName;
        }

        protected override async Task ExecuteAsync(CancellationToken cts)
        {
            // "Starting stream subscriber worker for Modbus Data"
            logger.LogInformation("Starting stream subscriber worker for T Data");

            while (!cts.IsCancellationRequested)
            {
                NamedPipeClientStream? pipe = null;

                try
                {
                    pipe = new NamedPipeClientStream(
                        ".",
                        this.pipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    logger.LogInformation($"Beginning connection on {pipeName}");
                    await pipe.ConnectAsync(cts);
                    logger.LogInformation($"Pipe connected on {pipeName}");

                    while (!cts.IsCancellationRequested && pipe.IsConnected)
                    {
                        logger.LogInformation($"Polling data on {pipeName}");
                        T? data = await subscriber.SubscribeAsync(pipe, cts);
                        logger.LogInformation($"Poll complete on {pipeName}");

                        if (data != null)
                        {
                            dataState.Update(data);
                        }
                        else
                        {
                            logger.LogInformation("Null data...");
                        }

                        await Task.Delay(100, cts); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
                    }
                }
                catch (IOException)
                {
                    logger.LogWarning($"Connection lost on {pipe}");
                }
                finally
                {
                    pipe?.Dispose();

                    // If either of the subscribers drops, we'll need to re-initialize
                    if (this.initState.IsInitialized)
                    {
                        this.initState.IsInitialized = false;
                        logger.LogInformation("Initialization State set to False!");
                    }
                }

                await Task.Delay(100, cts); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }
    }
}
