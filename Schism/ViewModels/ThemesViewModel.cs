// <copyright file="ThemesViewModel.cs" company="Precision Valve & Automation">
// Copyright (c) PVA. All rights reserved.
// </copyright>

using Schism.Services;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Schism.ViewModels
{
    public class ThemesViewModel : BindableBase
    {
        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // Private variable
        private string title;

        // Service Singleton that gets passed up to View
        public ThemeService TS => ThemeService.Instance;

        // Public instance with getters/setters
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        // Constructor
        public ThemesViewModel()
        {
            title = "Themes";
        }
    }
}
