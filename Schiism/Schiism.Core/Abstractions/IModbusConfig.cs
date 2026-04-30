// <copyright file="IModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Enums;

    /// <summary>
    /// Interface for defining what the MODBUS Configuration.
    /// Empty, since the main purpose of this is for Dependency Injection.
    /// </summary>
    public interface IModbusConfig
    {
        /// <summary>
        /// Gets or Sets IP Address.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Gets or Sets Data Length in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte DataLength { get; set; }

        /// <summary>
        /// Gets or Sets Starting Address.
        /// </summary>
        public ushort StartAddress { get; set; }

        /// <summary>
        /// Gets or Sets selected Data Size. May alter DataLength, depending on the entered value.
        /// </summary>
        public DataSize SelectedDataSize { get; set; }

        /// <summary>
        /// Gets or Sets TCP Port.
        /// </summary>
        public ushort TCPPort { get; set; }

        /// <summary>
        /// Gets or Sets Scan Rate.
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// Gets or Sets TCP Timeout.
        /// </summary>
        public int TCPTimeout { get; set; }

        /// <summary>
        /// Gets or Sets Device ID.
        /// </summary>
        public byte DeviceId { get; set; }

        /// <summary>
        /// Gets or Sets selected polling type.
        /// </summary>
        public PollType SelectedPollType { get; set; }

        /// <summary>
        /// Gets or Sets a value indicating whether ASCII display should be included or not.
        /// </summary>
        public bool AsciiEnable { get; set; }

        /// <summary>
        /// Gets or Sets selected numeric base.
        /// </summary>
        public NumericBase SelectedNumericBase { get; set; }

        /// <summary>
        /// Gets or Sets selected endian.
        /// </summary>
        public Endian SelectedEndian { get; set; }
    }
}
