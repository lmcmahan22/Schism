// <copyright file="ModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.Modbus
{
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using System;
    using System.Net;
    using System.Threading;

    /// <inheritdoc/>
    public class ModbusConfig : IModbusConfig
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
        public ModbusConfig()
        {
            iPAddress = "127.0.0.1"; // "192.168.100.20" for two device config. Otherwise, just use 127 for a single device localhost double duty build!
            dataLength = 10;
            startAddress = 0;
            selectedDataSize = DataSize.Bit16;
            tcpPort = 1502;
            scanRate = 1000;
            tcpTimeout = 5000;
            deviceId = 1;
            selectedPollType = PollType.CoilStatus;
            asciiEnable = false;
            selectedNumericBase = NumericBase.Decimal;
            selectedEndian = Endian.BigEndian;
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
        public ushort TCPPort { get => tcpPort; private set => tcpPort = value; }

        /// <inheritdoc/>
        public int ScanRate { get => scanRate; private set => scanRate = value; }

        /// <inheritdoc/>
        public int TCPTimeout { get => tcpTimeout; private set => tcpTimeout = value; }

        /// <inheritdoc/>
        public byte DeviceId { get => deviceId; private set => deviceId = value; }

        /// <inheritdoc/>
        public PollType SelectedPollType { get => selectedPollType; private set => selectedPollType = value; }

        /// <inheritdoc/>
        public bool AsciiEnable { get => asciiEnable; private set => asciiEnable = value; }

        /// <inheritdoc/>
        public NumericBase SelectedNumericBase { get => selectedNumericBase; private set => selectedNumericBase = value; }

        /// <inheritdoc/>
        public Endian SelectedEndian { get => selectedEndian; private set => selectedEndian = value; }

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
            lock (configLock)
            {
                if (cmd.IPAddress is not null)
                {
                    IPAddress = cmd.IPAddress;
                }

                if (cmd.TCPPort.HasValue)
                {
                    TCPPort = cmd.TCPPort.Value;
                }

                if (cmd.DeviceId.HasValue)
                {
                    DeviceId = cmd.DeviceId.Value;
                }

                if (cmd.StartAddress.HasValue)
                {
                    StartAddress = cmd.StartAddress.Value;
                }
                if (cmd.DataLength.HasValue)
                {
                    DataLength = cmd.DataLength.Value;
                }

                if (cmd.ScanRate.HasValue)
                {
                    ScanRate = cmd.ScanRate.Value;
                }

                if (cmd.TCPTimeout.HasValue)
                {
                    TCPTimeout = cmd.TCPTimeout.Value;
                }

                if (cmd.AsciiEnable.HasValue)
                {
                    AsciiEnable = cmd.AsciiEnable.Value;
                }

                if (cmd.SelectedDataSize.HasValue)
                {
                    SelectedDataSize = cmd.SelectedDataSize.Value;
                }

                if (cmd.SelectedPollType.HasValue)
                {
                    SelectedPollType = cmd.SelectedPollType.Value;
                }

                if (cmd.SelectedNumericBase.HasValue)
                {
                    SelectedNumericBase = cmd.SelectedNumericBase.Value;
                }

                if (cmd.SelectedEndian.HasValue)
                {
                    SelectedEndian = cmd.SelectedEndian.Value;
                }
            }
        }
    }
}
