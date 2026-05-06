namespace Schiism.Service.Models.Implementations.IPC
{
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.DTOs.IPC.Streams;
    using Schiism.Core.Models.Wrappers;
    using System.Threading.Channels;

    public class ConnDiagStreamQueue : IStreamQueue<ConnectionDiagnostics>
    {
        private readonly Channel<ConnectionDiagnostics> channel;

        private long enqueued;
        private long processed;
        private long dropped;

        private DateTime lastEnqueue;
        private DateTime lastDequeue;

        public ConnDiagStreamQueue()
        {
            this.channel = Channel.CreateBounded<ConnectionDiagnostics>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait, // backpressure
                SingleReader = true,
                SingleWriter = false,
            });
        }

        public async ValueTask EnqueueAsync(ConnectionDiagnostics info, CancellationToken ct)
        {
            lastEnqueue = DateTime.UtcNow;
            Interlocked.Increment(ref enqueued);

            var result = await channel.Writer.WaitToWriteAsync(ct);

            if (!result)
            {
                Interlocked.Increment(ref dropped);
                return;
            }

            await channel.Writer.WriteAsync(info, ct);
        }

        public IAsyncEnumerable<ConnectionDiagnostics> ReadAllAsync(CancellationToken ct)
            => ReadLoop(ct);

        private async IAsyncEnumerable<ConnectionDiagnostics> ReadLoop(
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