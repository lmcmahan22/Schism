using System.Text.Json.Serialization;

namespace Schiism.Core.Models.IPC.DTOs.Streams
{
    public record ModbusData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusData"/> class. Note that the parameters must match the record properties for JSON deserialization to work correctly (case-insenstitive).
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="data"></param>
        /// <param name="timestamp"></param>

        [JsonConstructor]
        public ModbusData(byte deviceId, List<string> data, DateTime timestamp)
        {
            DeviceId = deviceId;
            Data = data;
            Timestamp = timestamp;
        }

        public byte DeviceId { get; init; }

        public List<string> Data { get; init; }

        public DateTime Timestamp { get; init; }
    }
}
