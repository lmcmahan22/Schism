// <copyright file="MainWindowViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schism.ViewModels
{
    // The MainWindowViewModel class is a view model for the main window of the application.
    // It inherits from BindableBase, which provides support for property change notifications, allowing the view to update when properties in the view model change.
    public class MainWindowViewModel : BindableBase
    {
        // Private variable
        private string title;

        // Constructor
        public MainWindowViewModel()
        {
            this.title = "PVA MODBUS TCP Client";
        }

        // Public instance with getter/setter
        public string Title
        {
            get { return this.title; }
            set { this.SetProperty(ref this.title, value); }
        }
    }
}
