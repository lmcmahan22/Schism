// <copyright file="ThemesViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using Schiism.WPF.Services;

    public class ThemesViewModel : BindableBase
    {
        // Private variable
        private string title;

        // Constructor
        public ThemesViewModel()
        {
            title = "Themes";
        }

        // Service Singleton that gets passed up to View
        public ThemeController TS => ThemeController.Instance;

        // Public instance with getters/setters
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }
    }
}
