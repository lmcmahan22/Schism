// <copyright file="ConnSettingsViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Schism.ViewModels
{
    using System.Runtime.CompilerServices;
    using Schism.Services;

    public class ConnSettingsViewModel : BindableBase
    {
        // Private variable
        private string title;

        // Constructor
        public ConnSettingsViewModel()
        {
            this.title = "Connection Settings";
        }

        // Service Singleton that gets passed up to View
        public MODBUSService MS => MODBUSService.Instance;

        // Public instance with getter/setter
        public string Title
        {
            get => this.title;
            set => this.SetProperty(ref this.title, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.RaisePropertyChanged(propertyName);
        }
    }
}
