// <copyright file="ModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.Modbus
{
    using System;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <inheritdoc/>
    public class ConfigState : IConfigState
    {
        private readonly object configLock = new();

        // Private wariables (that which can be manipulated by more than one setter from this class, or non-nullable)
        private string iPAddress;
        private ushort tcpPort;
        private byte dataLength;
        private ushort startAddress;
        private DataSize selectedDataSize;
        private int scanRate;
        private int tcpTimeout;
        private byte deviceId;
        private PollType selectedPollType;
        private bool asciiEnable;
        private NumericBase selectedNumericBase;
        private Endian selectedEndian;

        /// <inheritdoc/>
        public ConfigState()
        {
            this.iPAddress = "127.0.0.1"; // "192.168.100.20" for two device config. Otherwise, just use 127 for a single device localhost double duty build!
            this.dataLength = 10;
            this.startAddress = 0;
            this.selectedDataSize = DataSize.Bit16;
            this.tcpPort = 1502;
            this.scanRate = 1000;
            this.tcpTimeout = 4000;
            this.deviceId = 1;
            this.selectedPollType = PollType.HoldingRegisters;
            this.asciiEnable = false;
            this.selectedNumericBase = NumericBase.Decimal;
            this.selectedEndian = Endian.BigEndian;
        }

        /// <inheritdoc/>
        public string IPAddress
        {
            get => this.iPAddress;
            set => this.iPAddress = value;
        }

        /// <summary>
        /// Gets dataLength in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte DataLength
        {
            get => this.dataLength;
            private set
            {
                byte minLen = this.GetMinLengthForDataSize();
                byte maxLen = this.GetMaxLengthForStartAddress();
                byte clampedDataLength = Math.Clamp(value, minLen, maxLen);

                if (this.dataLength != clampedDataLength)
                {
                    this.dataLength = clampedDataLength;
                }
            }
        }

        /// <summary>
        /// Gets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort StartAddress
        {
            get => this.startAddress;
            private set
            {
                // NOTE: THE FOLLOWING COMMENTED CODE SHOULD BE CONTROLLED IN THE WPF APPLICATION PRIOR TO RECIEPT BY THE ENGINE HERE!!!

                /* temp variable to help store the incoming decimal value, after possible hex conversion
                // uint attemptDecVal = 0;

                // StartAddress changed to ushort, because this string handling should be managed netirely in the UI
                // If the value contains "h"
                // if (value.Contains('h'))
                // {
                //    // Get rid of the "h" at the end ex. "Ah -> A"
                //    string trun = value.Substring(0, value.Length - 1);

                // convert hex string into a decimal int ex. "A -> 10"
                //    attemptDecVal = Convert.ToUInt32(trun, 16);
                // }

                // If the value contains just numbers (no "h")
                // else
                // {
                //    attemptDecVal = Convert.ToUInt32(value);
                // }

                // We can now confirm that the attempted decimal converted value is a short (1-65535), so we can type cast it!
                // ushort decVal = Convert.ToUInt16(attemptDecVal); */

                if (this.startAddress != value)
                {
                    this.startAddress = value;

                    byte maxLen = this.GetMaxLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(this.dataLength, (byte)1, maxLen);

                    if (this.dataLength != clampedDataLength)
                    {
                        this.dataLength = clampedDataLength;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public DataSize SelectedDataSize
        {
            get => this.selectedDataSize;
            private set
            {
                if (this.selectedDataSize != value)
                {
                    this.selectedDataSize = value;

                    byte minLen = this.GetMinLengthForDataSize();
                    byte clampedDataLength = Math.Clamp(this.dataLength, minLen, (byte)120);

                    if (this.dataLength != clampedDataLength)
                    {
                        this.dataLength = clampedDataLength;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public ushort TCPPort { get => this.tcpPort; private set => this.tcpPort = value; }

        /// <inheritdoc/>
        public int ScanRate { get => this.scanRate; private set => this.scanRate = value; }

        /// <inheritdoc/>
        public int TCPTimeout { get => this.tcpTimeout; private set => this.tcpTimeout = value; }

        /// <inheritdoc/>
        public byte DeviceId { get => this.deviceId; private set => this.deviceId = value; }

        /// <inheritdoc/>
        public PollType SelectedPollType { get => this.selectedPollType; private set => this.selectedPollType = value; }

        /// <inheritdoc/>
        public bool AsciiEnable { get => this.asciiEnable; private set => this.asciiEnable = value; }

        /// <inheritdoc/>
        public NumericBase SelectedNumericBase { get => this.selectedNumericBase; private set => this.selectedNumericBase = value; }

        /// <inheritdoc/>
        public Endian SelectedEndian { get => this.selectedEndian; private set => this.selectedEndian = value; }

        /// <inheritdoc/>
        public void Update(SettingsConfig cmd)
        {
            lock (this.configLock)
            {
                if (cmd.IPAddress is not null)
                {
                    this.IPAddress = cmd.IPAddress;
                }

                if (cmd.TCPPort.HasValue)
                {
                    this.TCPPort = cmd.TCPPort.Value;
                }

                if (cmd.DeviceId.HasValue)
                {
                    this.DeviceId = cmd.DeviceId.Value;
                }

                if (cmd.StartAddress.HasValue)
                {
                    this.StartAddress = cmd.StartAddress.Value;
                }

                if (cmd.DataLength.HasValue)
                {
                    this.DataLength = cmd.DataLength.Value;
                }

                if (cmd.ScanRate.HasValue)
                {
                    this.ScanRate = cmd.ScanRate.Value;
                }

                if (cmd.TCPTimeout.HasValue)
                {
                    this.TCPTimeout = cmd.TCPTimeout.Value;
                }

                if (cmd.AsciiEnable.HasValue)
                {
                    this.AsciiEnable = cmd.AsciiEnable.Value;
                }

                if (cmd.SelectedDataSize.HasValue)
                {
                    this.SelectedDataSize = cmd.SelectedDataSize.Value;
                }

                if (cmd.SelectedPollType.HasValue)
                {
                    this.SelectedPollType = cmd.SelectedPollType.Value;
                }

                if (cmd.SelectedNumericBase.HasValue)
                {
                    this.SelectedNumericBase = cmd.SelectedNumericBase.Value;
                }

                if (cmd.SelectedEndian.HasValue)
                {
                    this.SelectedEndian = cmd.SelectedEndian.Value;
                }
            }
        }

        // Prevent user from prompting a data overflow simply due to configuring the length and data size poorly
        private byte GetMinLengthForDataSize()
        {
            return this.selectedDataSize switch
            {
                DataSize.Bit32 => 2,
                DataSize.Bit64 => 4,
                _ => 1, // "Bit16" or default
            };
        }

        private byte GetMaxLengthForStartAddress()
        {
            int cap = ushort.MaxValue - this.startAddress + 1;
            ushort clamped = (ushort)Math.Min(120, cap);
            return (byte)clamped;
        }
    }
}
