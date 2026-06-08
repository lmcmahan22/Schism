// <copyright file="ModbusData.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.IPC.DTOs
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// ModbusData Record (immutable) to represent the data received from the Modbus device, along with the device ID and timestamp.
    /// This is the data structure that will be sent through the Modbus data stream queue for consumption by the FE or any other subscribers.
    /// </summary>
    public record ModbusDataDTO
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusDataDTO"/> class. Note that the parameters must match the record properties for JSON deserialization to work correctly (case-insenstitive).
        /// </summary>
        /// <param name="deviceId">The ID of the Modbus device.</param>
        /// <param name="data">The data from the Modbus device.</param>
        /// <param name="timestamp">The timestamp of the data.</param>
        [JsonConstructor]
        public ModbusDataDTO(byte deviceId, List<string> data, DateTime timestamp)
        {
            DeviceId = deviceId;
            Data = data;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the ID of the Modbus device.
        /// </summary>
        public byte DeviceId { get; init; }

        /// <summary>
        /// Gets the data from the Modbus device.
        /// </summary>
        public List<string> Data { get; init; }

        /// <summary>
        /// Gets the timestamp of the data.
        /// </summary>
        public DateTime Timestamp { get; init; }
    }
}
