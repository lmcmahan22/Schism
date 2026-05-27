// <copyright file="ModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Models.Implementations.States
{
    using System;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <inheritdoc/>
    public class WPFConfigState : BindableBase, IWPFConfigState
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

        private bool autoStart;
        private bool autoRestart;

        /// <inheritdoc/>
        public WPFConfigState()
        {
            iPAddress = "127.0.0.1"; // "192.168.100.20" for two device config. Otherwise, just use 127 for a single device localhost double duty build!
            dataLength = 15;
            startAddress = 0;
            selectedDataSize = DataSize.Bit16;
            tcpPort = 1502;
            scanRate = 2000;
            tcpTimeout = 5000;
            deviceId = 5;
            selectedPollType = PollType.CoilStatus;
            asciiEnable = true;
            selectedNumericBase = NumericBase.Hexadecimal;
            selectedEndian = Endian.LittleEndian;
            autoStart = false;
            autoRestart = false;
        }

        /// <inheritdoc/>
        public string IPAddress
        {
            get => iPAddress;
            set => SetProperty(ref iPAddress, value);
        }

        /// <summary>
        /// Gets dataLength in accordance with DataSize and StartAddress for min and max allowable value respectively.
        /// </summary>
        public byte DataLength
        {
            get => dataLength;
            set
            {
                byte minLen = GetMinLengthForDataSize();
                byte maxLen = GetMaxLengthForStartAddress();
                byte clampedDataLength = Math.Clamp(value, minLen, maxLen);

                if (dataLength != clampedDataLength)
                {
                    SetProperty(ref dataLength, clampedDataLength);
                }
            }
        }

        /// <summary>
        /// Gets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort StartAddress
        {
            get => startAddress;
            set
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
                    SetProperty(ref startAddress, value);

                    byte maxLen = GetMaxLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(dataLength, (byte)1, maxLen);

                    if (dataLength != clampedDataLength)
                    {
                        SetProperty(ref dataLength, clampedDataLength);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public DataSize SelectedDataSize
        {
            get => selectedDataSize;
            set
            {
                if (selectedDataSize != value)
                {
                    SetProperty(ref selectedDataSize, value);

                    byte minLen = GetMinLengthForDataSize();
                    byte clampedDataLength = Math.Clamp(dataLength, minLen, (byte)120);

                    if (dataLength != clampedDataLength)
                    {
                        SetProperty(ref dataLength, clampedDataLength);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public ushort TCPPort { get => tcpPort; set => SetProperty(ref tcpPort, value); }

        /// <inheritdoc/>
        public int ScanRate { get => scanRate; set => SetProperty(ref scanRate, value); }

        /// <inheritdoc/>
        public int TCPTimeout { get => tcpTimeout; set => SetProperty(ref tcpTimeout, value); }

        /// <inheritdoc/>
        public byte DeviceId { get => deviceId; set => SetProperty(ref deviceId, value); }

        /// <inheritdoc/>
        public PollType SelectedPollType { get => selectedPollType; set => SetProperty(ref selectedPollType, value); }

        /// <inheritdoc/>
        public bool AsciiEnable { get => asciiEnable; set => SetProperty(ref asciiEnable, value); }

        /// <inheritdoc/>
        public NumericBase SelectedNumericBase { get => selectedNumericBase; set => SetProperty(ref selectedNumericBase, value); }

        /// <inheritdoc/>
        public Endian SelectedEndian { get => selectedEndian; set => SetProperty(ref selectedEndian, value); }

        public bool AutoStart { get => autoStart; set => SetProperty(ref autoStart, value); }

        public bool AutoRestart { get => autoRestart; set => SetProperty(ref autoRestart, value); }

        /// <inheritdoc/>
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

                if (cmd.AutoStart.HasValue)
                {
                    AutoStart = cmd.AutoStart.Value;
                }

                if (cmd.AutoRestart.HasValue)
                {
                    AutoRestart = cmd.AutoRestart.Value;
                }
            }
        }

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
    }
}
