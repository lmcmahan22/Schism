// <copyright file="StreamPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Streams
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.IPC.Serialization;
    using System.IO.Pipes;

    /// <summary>
    /// Implementing class for the IStreamPublisher interface.
    /// </summary>
    /// <typeparam name="T">The object type sent along the stream.</typeparam>
    /// <param name="pipeName">Pipe name, DI'd.</param>
    /// <param name="initStatus">Frontend Initialized state object, DI'd.</param>
    /// <param name="logger">File Logger object, DI'd.</param>
    public class StreamPublisher<T>(PipeSerializer serializer, ILogger<StreamPublisher<T>> logger)
    {

        public async Task PublishAsync(
            PipeStream pipe,
            T data,
            CancellationToken ct)
        {
            try
            {
                await serializer.SerializeAsync(pipe, data, ct);
                await pipe.FlushAsync(ct);
                logger.LogInformation("[CORE] [STREAM] Published {0} data.", typeof(T).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CORE] [STREAM] {0} data publish failure. Error details: {1}", typeof(T).Name, ex.Message);
                throw;
            }
        }
    }
}
