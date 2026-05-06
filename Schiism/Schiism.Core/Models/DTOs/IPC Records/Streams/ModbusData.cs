using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Models.DTOs.IPC.Streams
{
    public record ModbusData
    {
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
