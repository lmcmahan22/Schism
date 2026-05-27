// <copyright file="IModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.States
{
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using System.ComponentModel;

    /// <summary>
    /// Interface for defining what the MODBUS Configuration contains.
    /// Empty, since the main purpose of this is for Dependency Injection.
    /// </summary>
    public interface IWPFConfigState : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets IP Address.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Gets Data Length in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte DataLength { get; set; }

        /// <summary>
        /// Gets Starting Address.
        /// </summary>
        public ushort StartAddress { get; set; }

        /// <summary>
        /// Gets selected Data Size. May alter DataLength, depending on the entered value.
        /// </summary>
        public DataSize SelectedDataSize { get; set; }

        /// <summary>
        /// Gets TCP Port.
        /// </summary>
        public ushort TCPPort { get; set; }

        /// <summary>
        /// Gets Scan Rate.
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// Gets TCP Timeout.
        /// </summary>
        public int TCPTimeout { get; set; }

        /// <summary>
        /// Gets Device ID.
        /// </summary>
        public byte DeviceId { get; set; }

        /// <summary>
        /// Gets selected polling type.
        /// </summary>
        public PollType SelectedPollType { get; set; }

        /// <summary>
        /// Gets a value indicating whether ASCII display should be included or not.
        /// </summary>
        public bool AsciiEnable { get; set; }

        /// <summary>
        /// Gets selected numeric base.
        /// </summary>
        public NumericBase SelectedNumericBase { get; set; }

        /// <summary>
        /// Gets selected endian.
        /// </summary>
        public Endian SelectedEndian { get; set; }

        public bool AutoStart { get; set; }

        public bool AutoRestart { get; set; }

        /// <summary>
        /// Updates the current configuration with the values from the provided configuration.
        /// </summary>
        /// <param name="cmd">The configuration to copy values from.</param>
        public void Update(SettingsConfig cmd);
    }
}
