using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schiism.Core.IPC.PipeControl;
using Schiism.Core.IPC.StateWrappers;
using Schiism.Core.IPC.Streams;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;

namespace Schiism.WPF.IPC
{
    public class StreamSubscriberWorker<T> : BackgroundService
    {
        // No in-line constructor here, in order to support the logger:
        private readonly ILogger logger;
        private readonly StreamSubscriber<T> subscriber;
        private InitStatus initStatus;
        private readonly string pipeName;
        private readonly StreamStore<T> streamStore;
        private INamedPipeFactory pipeFactory;

        // should this be a collection of ModbusData polls, just so we don't lose any data s we're trying to print it?
        private T rawData;

        public T RawData { get => rawData; }

        public StreamSubscriberWorker(string pipeName, INamedPipeFactory pipeFactory, StreamSubscriber<T> subscriber, StreamStore<T> streamStore, InitStatus initStatus, ILogger<StreamSubscriberWorker<T>> logger)
        {
            this.logger = logger;
            this.subscriber = subscriber;
            this.initStatus = initStatus;
            this.pipeName = pipeName;
            this.streamStore = streamStore;
            this.pipeFactory = pipeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken cts)
        {
            // "Starting stream subscriber worker for Modbus Data"
            logger.LogInformation("Starting stream subscriber worker for T Data");

            while (!cts.IsCancellationRequested)
            {
                using var pipe = pipeFactory.CreateNPClient(pipeName);

                try
                {
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
                            streamStore.Update(data);
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
                    if (initStatus.IsInitialized)
                    {
                        initStatus.IsInitialized = false;
                        logger.LogInformation("Initialization State set to False!");
                    }
                }

                await Task.Delay(100, cts); // IMPORTANT or you'll spin CPU. This should be short though, since there's no reason for the UI to wait for updated data if it has already arrived.
            }
        }
    }
}
