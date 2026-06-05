// <copyright file="StreamQueue.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Streams
{
    using System.Threading.Channels;

    /// <summary>
    /// Implementing class for the IStreamQueue interface, using System.Threading.Channels for efficient and thread-safe producer-consumer queues.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    public class StreamQueue<T>
    {
        private readonly Channel<T> channel;

        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamQueue{T}"/> class.
        /// Constructor initializes the channel object with a bounded capacity and appropriate options for a producer-consumer scenario.
        /// </summary>
        public StreamQueue()
        {
            channel = Channel.CreateBounded<T>(
                new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = false,
                    SingleWriter = false,
                });
        }

        /// <inheritdoc/>
        public ValueTask EnqueueAsync(T item, CancellationToken ct = default)
        {
            Count++;
            return channel.Writer.WriteAsync(item, ct);
        }

        /// <inheritdoc/>
        public ValueTask<T> DequeueAsync(CancellationToken ct = default)
        {
            Count--;
            return channel.Reader.ReadAsync(ct);
        }
    }
}