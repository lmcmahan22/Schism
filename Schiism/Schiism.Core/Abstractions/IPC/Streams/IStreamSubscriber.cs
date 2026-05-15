// <copyright file="IStreamSubscriber.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.Streams
{
    // Implemented by WPF
    public interface IStreamSubscriber<T>
    {
        Task SubscribeAsync(Func<T, Task> onData, CancellationToken ct);
    }
}
