// <copyright file="ThemesViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using Schiism.Services;

    public class ThemesViewModel : BindableBase
    {
        // Private variable
        private string title;

        // Constructor
        public ThemesViewModel()
        {
            this.title = "Themes";
        }

        // Service Singleton that gets passed up to View
        public ThemeService TS => ThemeService.Instance;

        // Public instance with getters/setters
        public string Title
        {
            get { return this.title; }
            set { this.SetProperty(ref this.title, value); }
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.RaisePropertyChanged(propertyName);
        }
    }
}
