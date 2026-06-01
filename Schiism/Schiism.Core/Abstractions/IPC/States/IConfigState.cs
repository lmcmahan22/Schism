// <copyright file="IModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.States
{
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// Interface for defining what the MODBUS Configuration contains.
    /// Empty, since the main purpose of this is for Dependency Injection.
    /// </summary>
    public interface IConfigState
    {
        /// <summary>
        /// Gets IP Address.
        /// </summary>
        public string IPAddress { get; }

        /// <summary>
        /// Gets Data Length in accordance with DataSize and StartAddress for min and max allowable values respectively.
        /// NOTE: Can NOT be controlled via UI.
        /// </summary>
        public ushort DataLength { get; }

        /// <summary>
        /// Gets Starting Address.
        /// </summary>
        public ushort StartAddress { get; }

        /// <summary>
        /// Gets selected Data Size. May alter DataLength, depending on the entered value.
        /// </summary>
        public DataSize SelectedDataSize { get; }

        /// <summary>
        /// Gets TCP Port.
        /// </summary>
        public ushort TCPPort { get; }

        /// <summary>
        /// Gets Scan Rate.
        /// </summary>
        public int ScanRate { get; }

        /// <summary>
        /// Gets TCP Timeout.
        /// </summary>
        public int TCPTimeout { get; }

        /// <summary>
        /// Gets Device ID.
        /// </summary>
        public byte DeviceId { get; }

        /// <summary>
        /// Gets selected polling type.
        /// </summary>
        public PollType SelectedPollType { get; }

        /// <summary>
        /// Gets a value indicating whether ASCII display should be included or not.
        /// </summary>
        public bool AsciiEnable { get; }

        /// <summary>
        /// Gets selected numeric base.
        /// </summary>
        public NumericBase SelectedNumericBase { get; }

        /// <summary>
        /// Gets selected endian.
        /// </summary>
        public Endian SelectedEndian { get; }

        /// <summary>
        /// Updates the current configuration with the values from the provided configuration.
        /// </summary>
        /// <param name="cmd">The configuration to copy values from.</param>
        public void Update(SettingsConfig cmd);
    }
}
