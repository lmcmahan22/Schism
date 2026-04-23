// <copyright file="ConnSettingsViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.ViewModels
{
    using System.Runtime.CompilerServices;
    using Schiism.Services;

    public class ConnSettingsViewModel : BindableBase
    {
        // Private variable
        private string title;

        // Constructor
        public ConnSettingsViewModel()
        {
            this.title = "Connection Settings";
        }

        // Public instance with getter/setter
        public string Title
        {
            get => this.title;
            set => this.SetProperty(ref this.title, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.RaisePropertyChanged(propertyName);
        }
    }
}
