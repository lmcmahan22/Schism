// <copyright file="IStreamPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.IO.Pipes;

namespace Schiism.Core.Abstractions.IPC.Streams
{
    /// <summary>
    /// Interface for the stream publisher (Windows Service to UI client).
    /// No looping done here, which is instead handled by the worker that DI's this publisher object.
    /// </summary>
    /// <typeparam name="T">The object type sent along the stream.</typeparam>
    public interface IStreamPublisher<T>
    {
        /// <summary>
        /// Publish data through the stream once (does not loop).
        /// </summary>
        /// <param name="data">The data published along the stream.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>Pass along the asynchronous task.</returns>
        Task PublishAsync(PipeStream pipe, T data, CancellationToken ct);
    }
}
