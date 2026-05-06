namespace Schiism.Core.Abstractions.IPC.Streams
{
    using Schiism.Core.Models.Wrappers;

    public interface IStreamQueue<T>
    {
        ValueTask EnqueueAsync(T data, CancellationToken ct);

        IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct);

        QueueMetrics Snapshot();
    }
}
