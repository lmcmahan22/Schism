// <copyright file="ModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations.Modbus
{
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.DTOs.IPC_Records.Commands;
    using Schiism.Core.Models.Enums;
    using System;
    using System.Net;
    using System.Threading;

    /// <inheritdoc/>
    public class ModbusConfig : IModbusConfig
    {

        private readonly object configLock = new();

        // Private wariables (that which can be manipulated by more than one setter from this class, or non-nullable)
        private string iPAddress;
        private byte dataLength;
        private ushort startAddress;
        private DataSize selectedDataSize;

        /// <inheritdoc/>
        public ModbusConfig()
        {
            iPAddress = "100.100.100.100";
            dataLength = 10;
            startAddress = 0;
            selectedDataSize = DataSize.Bit16;
        }

        /// <inheritdoc/>
        public string IPAddress
        {
            get => iPAddress;
            set => iPAddress = value;
        }

        /// <summary>
        /// Gets dataLength in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte DataLength
        {
            get => dataLength;
            private set
            {
                byte minLen = GetMinLengthForDataSize();
                byte maxLen = GetMaxLengthForStartAddress();
                byte clampedDataLength = Math.Clamp(value, minLen, maxLen);

                if (dataLength != clampedDataLength)
                {
                    dataLength = clampedDataLength;
                }
            }
        }

        /// <summary>
        /// Gets or Sets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort StartAddress
        {
            get => startAddress;
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

                if (startAddress != value)
                {
                    startAddress = value;

                    byte maxLen = GetMaxLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(dataLength, (byte)1, maxLen);

                    if (dataLength != clampedDataLength)
                    {
                        dataLength = clampedDataLength;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public DataSize SelectedDataSize
        {
            get => selectedDataSize;
            private set
            {
                if (selectedDataSize != value)
                {
                    selectedDataSize = value;

                    byte minLen = GetMinLengthForDataSize();
                    byte clampedDataLength = Math.Clamp(dataLength, minLen, (byte)120);

                    if (dataLength != clampedDataLength)
                    {
                        dataLength = clampedDataLength;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public ushort TCPPort { get; private set; }

        /// <inheritdoc/>
        public int ScanRate { get; private set; }

        /// <inheritdoc/>
        public int TCPTimeout { get; private set; }

        /// <inheritdoc/>
        public byte DeviceId { get; private set; }

        /// <inheritdoc/>
        public PollType SelectedPollType { get; private set; }

        /// <inheritdoc/>
        public bool AsciiEnable { get; private set; }

        /// <inheritdoc/>
        public NumericBase SelectedNumericBase { get; private set; }

        /// <inheritdoc/>
        public Endian SelectedEndian { get; private set; }

        // Prevent user from prompting a data overflow simply due to configuring the length and data size poorly
        private byte GetMinLengthForDataSize()
        {
            return selectedDataSize switch
            {
                DataSize.Bit32 => 2,
                DataSize.Bit64 => 4,
                _ => 1, // "Bit16" or default
            };
        }

        private byte GetMaxLengthForStartAddress()
        {
            int cap = ushort.MaxValue - startAddress + 1;
            ushort clamped = (ushort)Math.Min(120, cap);
            return (byte)clamped;
        }

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
    }
}
