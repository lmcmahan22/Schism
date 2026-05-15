// <copyright file="StreamQueue.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using System.Threading.Channels;
    using Schiism.Core.Abstractions.IPC.Streams;

    /// <summary>
    /// Implementing class for the IStreamQueue interface, using System.Threading.Channels for efficient and thread-safe producer-consumer queues.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    public class StreamQueue<T> : IStreamQueue<T>
    {
        private readonly Channel<T> channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamQueue{T}"/> class.
        /// Constructor initializes the channel object with a bounded capacity and appropriate options for a producer-consumer scenario.
        /// </summary>
        public StreamQueue()
        {
            this.channel = Channel.CreateBounded<T>(
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
            return this.channel.Writer.WriteAsync(item, ct);
        }

        /// <inheritdoc/>
        public ValueTask<T> DequeueAsync(CancellationToken ct = default)
        {
            return this.channel.Reader.ReadAsync(ct);
        }
    }
}