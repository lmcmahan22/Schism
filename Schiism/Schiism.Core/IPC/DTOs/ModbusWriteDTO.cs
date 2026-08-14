namespace Schiism.Core.IPC.DTOs
{
    using System.Text.Json.Serialization;
    using Schiism.Core.Configuration.Enums;

    public record ModbusWriteDTO
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusWriteDTO"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <param name="value"></param>
        [JsonConstructor]
        public ModbusWriteDTO(PollType type, byte deviceId, ushort address, string value)
        {
            Type = type;
            DeviceId = deviceId;
            Address = address;
            Value = value;
        }

        public PollType Type { get; init; }

        public byte DeviceId { get; init; }

        public ushort Address { get; init; }

        public string Value { get; init; }
    }
}
