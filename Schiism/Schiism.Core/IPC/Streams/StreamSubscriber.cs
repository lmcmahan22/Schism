// <copyright file="FEStreamSubscriber.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.Streams
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.Serialization;
    using System.IO.Pipes;

    /// <summary>
    /// Stream subscriber implementation for the front end.
    /// </summary>
    /// <typeparam name="T">Defines the object type that will be expected on this stream.</typeparam>
    /// <param name="pipeName">Name of pipe that the stream data will be received from.</param>
    public class StreamSubscriber<T>(PipeSerializer serializer, ILogger<StreamSubscriber<T>> logger)
    {
        /// <inheritdoc/>
        public async Task<T?> SubscribeAsync(PipeStream pipe, CancellationToken ct)
        {
            logger.LogInformation($"Deserializing on {pipe}");
            T? data = await serializer.DeserializeAsync<T>(pipe, ct);

            if (typeof(T).Name == "ModbusData")
            {
                ModbusDataCollectionDTO? modData = data as ModbusDataCollectionDTO;

                logger.LogInformation($"Received data on {typeof(T).Name} pipe: {data}.");

                string output = string.Empty;
                for (int i = 0; i < modData.Data.Count; i++)
                {
                    output += modData.Data[i].ToString() + ", ";
                }

                logger.LogInformation($"Modbus Data: {output}");
            }
            else
            {
                logger.LogInformation($"Received data on {typeof(T).Name} pipe: {data}.");
            }

            return data;
        }

        public void Update()
        {

        }
    }
}