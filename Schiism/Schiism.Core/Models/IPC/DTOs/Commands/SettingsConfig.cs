// <copyright file="SettingsConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.IPC.DTOs.Commands
{
    using System.Text.Json.Serialization;
    using Schiism.Core.Enums;

    /// <summary>
    /// Only sends what is updated! Keeps messages concise.
    /// </summary>
    public record SettingsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsConfig"/> class.
        /// </summary>
        /// <param name="iPAddress">The IP address.</param>
        /// <param name="dataLength">The amount of coils/registers to be read.</param>
        /// <param name="startAddress">The starting address to read data from (inclusive).</param>
        /// <param name="tCPPort">The TCP port.</param>
        /// <param name="scanRate">The amount of time delayed between polls (in milliseconds).</param>
        /// <param name="tCPTimeout">The TCP timeout (in milliseconds).</param>
        /// <param name="deviceId">The Modbus device ID, in case there are multiple on the network.</param>
        /// <param name="selectedDataSize">The selected data size (e.g., 16, 32, 64 bit).</param>
        /// <param name="selectedPollType">The selected poll type (e.g., Read Coils, Read Holding Registers).</param>
        /// <param name="asciiEnable">Indicates if ASCII is enabled.</param>
        /// <param name="selectedNumericBase">The selected numeric base (e.g., Decimal, Hexadecimal).</param>
        /// <param name="selectedEndian">The selected endian type (e.g., Little Endian, Big Endian).</param>
        [JsonConstructor]
        public SettingsConfig(string? iPAddress, byte? dataLength, ushort? startAddress, ushort? tCPPort, int? scanRate, int? tCPTimeout, byte? deviceId, DataSize? selectedDataSize, PollType? selectedPollType, bool? asciiEnable, NumericBase? selectedNumericBase, Endian? selectedEndian, bool? autoStart, bool? autoRestart)
        {
            this.IPAddress = iPAddress;
            this.DataLength = dataLength;
            this.StartAddress = startAddress;
            this.TCPPort = tCPPort;
            this.ScanRate = scanRate;
            this.TCPTimeout = tCPTimeout;
            this.DeviceId = deviceId;
            this.SelectedDataSize = selectedDataSize;
            this.SelectedPollType = selectedPollType;
            this.AsciiEnable = asciiEnable;
            this.SelectedNumericBase = selectedNumericBase;
            this.SelectedEndian = selectedEndian;
            this.AutoStart = autoStart;
            this.AutoRestart = autoRestart;
        }

        /// <summary>
        /// Gets the IP Address of the Modbus device. Should be in IPv4 format (e.g., "100.100.100.100").
        /// </summary>
        public string? IPAddress { get; init; }

        /// <summary>
        /// Gets Data Length in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte? DataLength { get; init; }

        /// <summary>
        /// Gets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort? StartAddress { get; init; }

        /// <summary>
        /// Gets the TCP Port of the Modbus device. Should be between 1 and 65535.
        /// </summary>
        public ushort? TCPPort { get; init; }

        /// <summary>
        /// Gets the Scan Rate, which is the amount of time delayed between polls, in milliseconds. Should be a positive integer.
        /// </summary>
        public int? ScanRate { get; init; }

        /// <summary>
        /// Gets the TCP Timeout, which is the amount of time to wait for a response before considering the attempt a failure, in milliseconds. Should be a positive integer.
        /// </summary>
        public int? TCPTimeout { get; init; }

        /// <summary>
        /// Gets the Modbus device ID, in case there are multiple devices on the network. Should be a positive integer.
        /// </summary>
        public byte? DeviceId { get; init; }

        /// <summary>
        /// Gets the selected data size, which determines how many bits are read for each data point.
        /// </summary>
        public DataSize? SelectedDataSize { get; init; }

        /// <summary>
        /// Gets the selected poll type, which determines the Modbus function used for polling (e.g., Read Coils, Read Holding Registers).
        /// </summary>
        public PollType? SelectedPollType { get; init; }

        /// <summary>
        /// Gets flag indicating whether ASCII mode is enabled, which may affect how data is encoded/decoded during communication.
        /// </summary>
        public bool? AsciiEnable { get; init; }

        /// <summary>
        /// Gets the selected numeric base, which determines how numeric values are represented (e.g., Decimal, Hexadecimal).
        /// </summary>
        public NumericBase? SelectedNumericBase { get; init; }

        /// <summary>
        /// Gets the selected endian type, which determines the byte order used for interpreting multi-byte data (e.g., Little Endian, Big Endian).
        /// </summary>
        public Endian? SelectedEndian { get; init; }

        public bool? AutoStart { get; init; }

        public bool? AutoRestart { get; init; }
    }
}
