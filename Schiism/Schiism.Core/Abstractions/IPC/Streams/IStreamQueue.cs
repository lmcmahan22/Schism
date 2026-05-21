// <copyright file="IStreamQueue.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.Streams
{
    /// <summary>
    /// Interface for a stream queue, which is a thread-safe producer-consumer queue used for streaming data between the service and the FE or any other subscribers.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    public interface IStreamQueue<T>
    {

        public int Count { get; }

        /// <summary>
        /// Method for adding an item to the queue. This method is asynchronous and can be awaited. It also accepts a cancellation token to allow for cancellation of the enqueue operation.
        /// </summary>
        /// <param name="data">The item to add to the queue.</param>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        /// <returns>A ValueTask representing the asynchronous operation.</returns>
        ValueTask EnqueueAsync(T data, CancellationToken ct);

        /// <summary>
        /// Method for removing an item from the queue. This method is asynchronous and can be awaited. It also accepts a cancellation token to allow for cancellation of the dequeue operation.
        /// </summary>
        /// <param name="ct">The cancellation token to cancel the operation.</param>
        /// <returns>A ValueTask representing the asynchronous operation, containing the dequeued item.</returns>
        ValueTask<T> DequeueAsync(CancellationToken ct = default);
    }
}
