// <copyright file="BoardAvailableViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.WPF.Models;
    using System.Runtime.CompilerServices;

    public class WriteValueViewModel : BindableBase
    {
        // Private variable
        private string title;
        private string address = string.Empty;
        private string value = string.Empty;

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
            // Raise event
            this.MWState.Address = Convert.ToUInt16(this.Address);
            this.MWState.Value = this.Value;
            this.MWState.TriggerSend();

            // Clear UI values
            this.Address = string.Empty;
            this.Value = string.Empty;
        }
    }
}
