// <copyright file="BoardAvailableViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.Core.Configuration.Enums;
    using Schiism.WPF.Models;
    using System.Runtime.CompilerServices;

    public class WriteValueViewModel : BindableBase
    {
        // Private variable
        private string title;
        private string address = string.Empty;
        private string value = string.Empty;

        private bool sendReg = false;

        private DelegateCommand? writeClick;

        public DelegateCommand WriteClick =>
            writeClick ??= new DelegateCommand(ExecuteWriteClick);

        public ModbusWriteState MWState { get; }

        // Constructor
        public WriteValueViewModel(ModbusWriteState mWState)
        {
            MWState = mWState;
            title = "Write Single Value";
        }

        // Public instance with getter/setter
        public string Title
        {
            get => title;
            set => SetProperty(ref this.title, value);
        }

        public string Address
        {
            get => this.address;
            set => SetProperty(ref this.address, value);
        }

        public bool SendReg
        {
            get => this.sendReg;
            set => SetProperty(ref this.sendReg, value);
        }

        public string Value
        {
            get => this.value;
            set => SetProperty(ref this.value, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        private void ExecuteWriteClick()
        {
            // Convert SendReg from bool to enum
            PollType typeToSend;

            if (SendReg)
            {
                typeToSend = PollType.HoldingRegisters;
            }
            else
            {
                typeToSend = PollType.CoilStatus;
            }

            // Send values and raise event
            this.MWState.SendWrite(typeToSend, Convert.ToUInt16(this.Address), this.Value);

            // Clear UI values
            this.Address = string.Empty;
            this.Value = string.Empty;
        }
    }
}
