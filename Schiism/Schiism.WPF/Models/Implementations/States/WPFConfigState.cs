// <copyright file="ModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Models.Implementations.States
{
    using System;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Commands;
    using Schiism.WPF.Models.Enums;

    /// <inheritdoc/>
    public class WPFConfigState : BindableBase, IWPFConfigState
    {
        private readonly object configLock = new();

        // Private wariables (that which can be manipulated by more than one setter from this class, or non-nullable)
        private ushort dataLength;

        private string iPAddress;
        private ushort tcpPort;
        private ushort startAddress;
        private DataSize selectedDataSize;
        private int scanRate;
        private int tcpTimeout;
        private byte deviceId;
        private PollType selectedPollType;
        private bool asciiEnable;
        private NumericBase selectedNumericBase;
        private Endian selectedEndian;

        private AddressConvention selectedAddressConvention;

        private bool autoStart;
        private bool autoRestart;

        /// <inheritdoc/>
        public WPFConfigState()
        {
            iPAddress = "127.0.0.1"; // "192.168.100.20" for two device config. Otherwise, just use 127 for a single device localhost double duty build!
            startAddress = 0;
            selectedDataSize = DataSize.Bit16;
            tcpPort = 1502;
            scanRate = 2000;
            tcpTimeout = 5000;
            deviceId = 1;
            selectedPollType = PollType.CoilStatus;
            asciiEnable = false;
            selectedNumericBase = NumericBase.Decimal;
            selectedEndian = Endian.BigEndian;
            this.selectedAddressConvention = AddressConvention.RegisterAddress;
            autoStart = false;
            autoRestart = false;
        }

        /// <inheritdoc/>
        public string IPAddress
        {
            get => iPAddress;
            set => SetProperty(ref iPAddress, value);
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
                }
            }
        }

        /// <inheritdoc/>
        public ushort TCPPort { get => tcpPort; set => SetProperty(ref tcpPort, value); }

        public ushort DataLength { get => dataLength; set => SetProperty(ref dataLength, value); }

        public ushort StartAddress { get => startAddress; set => SetProperty(ref startAddress, value); }

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

        // Does not get communicated to/from Service. Only here for multi-ViewModel control.
        public AddressConvention SelectedAddressConvention { get => selectedAddressConvention; set => SetProperty(ref selectedAddressConvention, value); }

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
                    // NOTE: This only gets set via the initial config! Does not get set via UI.
                    // This is here so the WPF knows how to assess the polling length in accordance with displaying the MODBUS table.
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
    }
}
