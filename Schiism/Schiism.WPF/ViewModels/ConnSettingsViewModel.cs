// <copyright file="ConnSettingsViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.Core.Abstractions.IPC.States;
    using System.Runtime.CompilerServices;

    public class ConnSettingsViewModel : BindableBase
    {
        // Private variable
        private string title;

        public IWPFConfigState ModbusSettState { get; }

        // Constructor
        public ConnSettingsViewModel(IWPFConfigState ModbusSettState)
        {
            title = "Connection Settings";
            this.ModbusSettState = ModbusSettState;
        }

        // Public instance with getter/setter
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }
    }
}
