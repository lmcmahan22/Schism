using Schiism.Core.Configuration.Enums;
using Schiism.Core.IPC.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Schiism.Core.Configuration.StateControl
{
    // Invokes INotifyPropertyChanged for frontend and backend use, since both apps subscribe to changes on this object, just not always for UI updates.
    public class ConfigState : INotifyPropertyChanged
    {
        // Private wariables (that which can be manipulated by more than one setter from this class, or non-nullable)
        private string iPAddress;
        private ushort tcpPort;
        private ushort dataLength;
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
        public ConfigState()
        {
            iPAddress = "127.0.0.1"; // "192.168.100.20" for two device config. Otherwise, just use 127 for a single device localhost double duty build!
            dataLength = 100;
            startAddress = 0;
            selectedDataSize = DataSize.Bit16;
            tcpPort = 1502;
            scanRate = 1000;
            tcpTimeout = 4000;
            deviceId = 1;
            selectedPollType = PollType.CoilStatus;
            asciiEnable = false;
            selectedNumericBase = NumericBase.Decimal;
            selectedEndian = Endian.BigEndian;
            autoStart = true;
            autoRestart = true;
        }

        // Notifies for subscriptions
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc/>
        public string IPAddress
        {
            get => iPAddress;
            set => iPAddress = value;
        }

        /// <summary>
        /// </summary>
        public ushort DataLength { get => dataLength; set => dataLength = value; }

        /// <summary>
        /// Gets Starting Address. May alter DataLength, depending on the entered value.
        /// </summary>
        public ushort StartAddress { get => startAddress; set => startAddress = value; }

        /// <inheritdoc/>
        public DataSize SelectedDataSize { get => selectedDataSize; set => selectedDataSize = value; }

        /// <inheritdoc/>
        public ushort TCPPort { get => tcpPort; set => tcpPort = value; }

        /// <inheritdoc/>
        public int ScanRate { get => scanRate; set => scanRate = value; }

        /// <inheritdoc/>
        public int TCPTimeout { get => tcpTimeout; set => tcpTimeout = value; }

        /// <inheritdoc/>
        public byte DeviceId { get => deviceId; set => deviceId = value; }

        /// <inheritdoc/>
        public PollType SelectedPollType { get => selectedPollType; set => selectedPollType = value; }

        /// <inheritdoc/>
        public bool AsciiEnable { get => asciiEnable; set => asciiEnable = value; }

        /// <inheritdoc/>
        public NumericBase SelectedNumericBase { get => selectedNumericBase; set => selectedNumericBase = value; }

        /// <inheritdoc/>
        public Endian SelectedEndian { get => selectedEndian; set => selectedEndian = value; }

        public bool AutoStart { get => autoStart; set => autoStart = value; }

        public bool AutoRestart { get => autoRestart; set => autoRestart = value; }

        public void Update(SettingsConfig cmd)
        {
            if (cmd.IPAddress != null)
            {
                IPAddress = cmd.IPAddress;
            }

            if (cmd.DataLength.HasValue)
            {
                DataLength = cmd.DataLength.Value;
            }

            if (cmd.StartAddress.HasValue)
            {
                StartAddress = cmd.StartAddress.Value;
            }

            if (cmd.TCPPort.HasValue)
            {
                TCPPort = cmd.TCPPort.Value;
            }

            if (cmd.ScanRate.HasValue)
            {
                ScanRate = cmd.ScanRate.Value;
            }

            if (cmd.TCPTimeout.HasValue)
            {
                TCPTimeout = cmd.TCPTimeout.Value;
            }

            if (cmd.DeviceId.HasValue)
            {
                DeviceId = cmd.DeviceId.Value;
            }

            if (cmd.SelectedDataSize.HasValue)
            {
                SelectedDataSize = cmd.SelectedDataSize.Value;
            }

            if (cmd.SelectedPollType.HasValue)
            {
                SelectedPollType = cmd.SelectedPollType.Value;
            }

            if (cmd.AsciiEnable.HasValue)
            {
                AsciiEnable = cmd.AsciiEnable.Value;
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

        public SettingsConfig Push()
        {
            return new SettingsConfig(
                IPAddress,
                DataLength,
                StartAddress,
                TCPPort,
                ScanRate,
                TCPTimeout,
                DeviceId,
                SelectedDataSize,
                SelectedPollType,
                AsciiEnable,
                SelectedNumericBase,
                SelectedEndian,
                AutoStart,
                AutoRestart);
        }

        // // Prevent user from prompting a data overflow simply due to configuring the length and data size poorly
        // private ushort GetMinLengthForDataSize()
        // {
        //    return selectedDataSize switch
        //    {
        //        DataSize.Bit32 => 2,
        //        DataSize.Bit64 => 4,
        //        _ => 1, // "Bit16" or default
        //    };
        // }

        // private ushort GetMaxLengthForStartAddress()
        // {
        //    int cap = ushort.MaxValue - startAddress + 1;
        //    ushort clamped = (ushort)Math.Min(120, cap);
        //    return clamped;
        // }
    }
}
