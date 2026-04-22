// <copyright file="MainWindowViewModel.cs" company="Precision Valve & Automation">
// Copyright (c) PVA. All rights reserved.
// </copyright>

namespace Schism.ViewModels
{
    // The MainWindowViewModel class is a view model for the main window of the application.
    // It inherits from BindableBase, which provides support for property change notifications, allowing the view to update when properties in the view model change.
    public class MainWindowViewModel : BindableBase
    {

        // Private variable 
        private string title;

        // Public instance with getter/setter
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        // Constructor
        public MainWindowViewModel()
        {
            title = "PVA MODBUS TCP Client";
        }
    }
}
