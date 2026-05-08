namespace Schiism.Service.Models.Implementations.IPC.Queues
{
    using System.Threading.Channels;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.DTOs.IPC_Records.Streams;
    using Schiism.Core.Models.Wrappers;

    public class ModbusStreamQueue : IStreamQueue<ModbusData>
    {
        private readonly Channel<ModbusData> channel;

        private long enqueued;
        private long processed;
        private long dropped;

        private DateTime lastEnqueue;
        private DateTime lastDequeue;

        public ModbusStreamQueue()
        {
            channel = Channel.CreateBounded<ModbusData>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait, // backpressure
                SingleReader = true,
                SingleWriter = false,
            });
        }

        public async ValueTask EnqueueAsync(ModbusData data, CancellationToken ct)
        {
            lastEnqueue = DateTime.UtcNow;
            Interlocked.Increment(ref enqueued);

            var result = await channel.Writer.WaitToWriteAsync(ct);

            if (!result)
            {
                Interlocked.Increment(ref dropped);
                return;
            }

            await channel.Writer.WriteAsync(data, ct);
        }

        public IAsyncEnumerable<ModbusData> ReadAllAsync(CancellationToken ct)
            => ReadLoop(ct);

        private async IAsyncEnumerable<ModbusData> ReadLoop(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
            {
                lastDequeue = DateTime.UtcNow;
                Interlocked.Increment(ref processed);
                yield return item;
            }
        }

        public QueueMetrics Snapshot()
        {
            return new QueueMetrics(
                channel.Reader.Count,
                Interlocked.Read(ref enqueued),
                Interlocked.Read(ref processed),
                Interlocked.Read(ref dropped),
                lastEnqueue,
                lastDequeue);
        }
    }
}