// <copyright file="ConnSettingsViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.Core.Configuration;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Net;
    using System.Runtime.CompilerServices;

    public class ConnSettingsViewModel : BindableBase
    {
        // Private variable
        private string title;

        private string iPAddress;
        private ushort tcpPort;
        private int scanRate;
        private int tcpTimeout;
        private bool autoStart;
        private bool autoRestart;

        private DelegateCommand? applyClick;

        public DelegateCommand ApplyClick =>
        applyClick ??= new DelegateCommand(ExecuteApplyClick);

        public ConfigState ModbusSettState { get; }

        // Constructor
        public ConnSettingsViewModel(ConfigState ModbusSettState)
        {
            title = "Connection Settings";
            this.ModbusSettState = ModbusSettState;

            // If the init setting already fired before the viewModel was created
            InitUpdate();

            // Vice versa from above
            ModbusSettState.PropertyChanged += InitSettReceived;
        }

        private void InitSettReceived(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ConfigState.IPAddress):
                case nameof(ConfigState.TCPPort):
                case nameof(ConfigState.ScanRate):
                case nameof(ConfigState.TCPTimeout):
                case nameof(ConfigState.AutoStart):
                case nameof(ConfigState.AutoRestart):
                    InitUpdate();
                    break;
            }
        }

        // Public instance with getter/setter
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public string IPAddress
        {
            get => iPAddress;
            set => SetProperty(ref iPAddress, value);
        }

        public ushort TCPPort
        {
            get => tcpPort;
            set => SetProperty(ref tcpPort, value);
        }

        public int ScanRate
        {
            get => scanRate;
            set => SetProperty(ref scanRate, value);
        }

        public int TCPTimeout
        {
            get => tcpTimeout;
            set => SetProperty(ref tcpTimeout, value);
        }

        public bool AutoStart
        {
            get => autoStart;
            set => SetProperty(ref autoStart, value);
        }

        public bool AutoRestart
        {
            get => autoRestart;
            set => SetProperty(ref autoRestart, value);
        }

        public void updateSett(string iPAddress, ushort tCPPort, int scanRate, int tCPTimeout, bool autoStart, bool autoRestart)
        {
            this.IPAddress = iPAddress;
            this.TCPPort = tCPPort;
            this.ScanRate = scanRate;
            this.TCPTimeout = tCPTimeout;
            this.AutoStart = autoStart;
            this.AutoRestart = autoRestart;
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        private void ExecuteApplyClick()
        {
            this.ModbusSettState.setMS(this.IPAddress, this.TCPPort, this.ScanRate, this.TCPTimeout, this.AutoStart, this.AutoRestart);
            this.ModbusSettState.TriggerApply();
        }

        private void InitUpdate()
        {
            IPAddress = ModbusSettState.IPAddress;
            TCPPort = ModbusSettState.TCPPort;
            ScanRate = ModbusSettState.ScanRate;
            TCPTimeout = ModbusSettState.TCPTimeout;
            AutoStart = ModbusSettState.AutoStart;
            AutoRestart = ModbusSettState.AutoRestart;
        }
    }
}
