// <copyright file="StreamPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using System.IO.Pipes;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Models.IPC;

    /// <summary>
    /// Implementing class for the IStreamPublisher interface.
    /// </summary>
    /// <typeparam name="T">The object type sent along the stream.</typeparam>
    /// <param name="pipeName">Pipe name, DI'd.</param>
    /// <param name="fEInitState">Frontend Initialized state object, DI'd.</param>
    /// <param name="logger">File Logger object, DI'd.</param>
    public class ServiceStreamPublisher<T>(ILogger<ServiceStreamPublisher<T>> logger) : IStreamPublisher<T>
    {
        private readonly PipeSerializer serializer = new();

        public async Task PublishAsync(
            PipeStream pipe,
            T data,
            CancellationToken ct)
        {
            try
            {
                await serializer.SerializeAsync(pipe, data, ct);
                await pipe.FlushAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogInformation(
                    ex,
                    $"Publish attempt failed on pipe.");
                throw;
            }
        }
    }
}
