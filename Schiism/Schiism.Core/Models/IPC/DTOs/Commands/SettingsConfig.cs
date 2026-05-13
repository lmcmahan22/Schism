using Schiism.Core.Abstractions.Modbus;
using Schiism.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Schiism.Core.Models.IPC.DTOs.Commands
{
    /// <summary>
    /// Only sends what is updated! Keeps messages concise.
    /// </summary>
    public record SettingsConfig
    {
        [JsonConstructor]
        public SettingsConfig(string? iPAddress, byte? dataLength, ushort? startAddress, ushort? tCPPort, int? scanRate, int? tCPTimeout, byte? deviceId, DataSize? selectedDataSize, PollType? selectedPollType, bool? asciiEnable, NumericBase? selectedNumericBase, Endian? selectedEndian)
        {
            IPAddress = iPAddress;
            DataLength = dataLength;
            StartAddress = startAddress;
            TCPPort = tCPPort;
            ScanRate = scanRate;
            TCPTimeout = tCPTimeout;
            DeviceId = deviceId;
            SelectedDataSize = selectedDataSize;
            SelectedPollType = selectedPollType;
            AsciiEnable = asciiEnable;
            SelectedNumericBase = selectedNumericBase;
            SelectedEndian = selectedEndian;
        }

        /// <inheritdoc/>
        public string? IPAddress { get; init; }

        /// <summary>
        /// Gets or Sets Data Length in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte? DataLength { get; init; }
        /// <summary>
        /// Gets or Sets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort? StartAddress { get; init; }

        /// <inheritdoc/>
        public ushort? TCPPort { get; init; }

        /// <inheritdoc/>
        public int? ScanRate { get; init; }

        /// <inheritdoc/>
        public int? TCPTimeout { get; init; }

        /// <inheritdoc/>
        public byte? DeviceId { get; init; }

        /// <inheritdoc/>
        public DataSize? SelectedDataSize { get; init; }

        /// <inheritdoc/>
        public PollType? SelectedPollType { get; init; }

        /// <inheritdoc/>
        public bool? AsciiEnable { get; init; }

        /// <inheritdoc/>
        public NumericBase? SelectedNumericBase { get; init; }

        /// <inheritdoc/>
        public Endian? SelectedEndian { get; init; }
    }
}
