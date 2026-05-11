// <copyright file="StreamQueue.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations.IPC.Queues
{
    using Schiism.Core.Abstractions.IPC.Streams;
    using System.Threading.Channels;

    public class StreamQueue<T> : IStreamQueue<T>
    {
        private readonly Channel<T> channel;

        public StreamQueue()
        {
            // Channel is very useful! It provides a thread-safe way to handle producer-consumer scenarios without the need for explicit locking.
            // This also sets a hard limit of 1000 items in the queue to prevent memory overload.
            this.channel = Channel.CreateBounded<T>(
                new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = false,
                    SingleWriter = false,
                });
        }

        public ValueTask EnqueueAsync(T item, CancellationToken ct = default)
        {
            return this.channel.Writer.WriteAsync(item, ct);
        }

        public ValueTask<T> DequeueAsync(CancellationToken ct = default)
        {
            return this.channel.Reader.ReadAsync(ct);
        }
    }
}