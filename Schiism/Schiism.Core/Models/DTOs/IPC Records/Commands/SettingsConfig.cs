using Schiism.Core.Abstractions.Modbus;
using Schiism.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Models.DTOs.IPC_Records.Commands
{
    /// <summary>
    /// Only sends what is updated! Keeps messages concise.
    /// </summary>
    public class SettingsConfig
    {
        /// <inheritdoc/>
        public string? IPAddress { get; set; }

        /// <summary>
        /// Gets or Sets Data Length in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte? DataLength { get; set; }

        /// <summary>
        /// Gets or Sets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort? StartAddress { get; set; }

        /// <inheritdoc/>
        public ushort? TCPPort { get; set; }

        /// <inheritdoc/>
        public int? ScanRate { get; set; }

        /// <inheritdoc/>
        public int? TCPTimeout { get; set; }

        /// <inheritdoc/>
        public byte? DeviceId { get; set; }

        /// <inheritdoc/>
        public DataSize? SelectedDataSize { get; set; }

        /// <inheritdoc/>
        public PollType? SelectedPollType { get; set; }

        /// <inheritdoc/>
        public bool? AsciiEnable { get; set; }

        /// <inheritdoc/>
        public NumericBase? SelectedNumericBase { get; set; }

        /// <inheritdoc/>
        public Endian? SelectedEndian { get; set; }
    }
}
