namespace Schiism.Core.Abstractions.IPC.Streams
{
    public interface IStreamQueue<T>
    {
        ValueTask EnqueueAsync(T data, CancellationToken ct);

        ValueTask<T> DequeueAsync(CancellationToken ct = default);
    }
}
