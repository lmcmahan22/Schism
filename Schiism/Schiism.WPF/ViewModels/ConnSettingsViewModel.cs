// <copyright file="ConnSettingsViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using System.Runtime.CompilerServices;

    public class ConnSettingsViewModel : BindableBase
    {
        // Private variable
        private string title;

        // Constructor
        public ConnSettingsViewModel()
        {
            title = "Connection Settings";
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
