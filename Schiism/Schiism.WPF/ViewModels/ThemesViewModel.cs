// <copyright file="ThemesViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using Schiism.WPF.Tabs;
    using System.Runtime.CompilerServices;

    public class ThemesViewModel : BindableBase
    {
        // Private variable
        private string title;

        public ThemesControl ThemeService { get; }

        // Constructor
        public ThemesViewModel(ThemesControl ThemeService)
        {
            title = "Themes";
            this.ThemeService = ThemeService;
        }

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
