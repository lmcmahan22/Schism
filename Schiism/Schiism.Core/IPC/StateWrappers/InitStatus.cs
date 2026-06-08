// <copyright file="FrontendInitState.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.ComponentModel;

namespace Schiism.Core.IPC.StateWrappers
{
    /// <inheritdoc/>
    public class InitStatus : INotifyPropertyChanged
    {
        private volatile bool isInitialized;

        public InitStatus()
        {
            IsInitialized = false;
        }

        /// <summary>
        /// "Initialization" is defined as the state in which the Service has sent the Initial Settings DTO, and the Frontend has accepted it. If one app goes down, this status falls.
        /// </summary>
        public bool IsInitialized
        {
            get => isInitialized;
            set
            {
                isInitialized = value;
                OnPropertyChanged(nameof(IsInitialized));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
