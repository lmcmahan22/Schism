// <copyright file="AboutViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.ViewModels
{
    using System.Runtime.CompilerServices;

    public class AboutViewModel : BindableBase
    {
        // Private variables
        private string title;
        private string version;
        private string buildDate;
        private string appName;
        private string copyright;
        private string author;

        // Constructor
        public AboutViewModel()
        {
            title = "About";
            version = "Version: 1.0.0";
            buildDate = "Build Date: 04/20/2026";
            appName = "MODBUS TCP Client Simulator";
            copyright = "©2026 Precision Valve & Automation, Inc.";
            author = "Author: Liam McMahan (Product Development)";
        }

        // Public instances with getters/setters
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public string Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        public string AppName
        {
            get => appName;
            set => SetProperty(ref appName, value);
        }

        public string Copyright
        {
            get => copyright;
            set => SetProperty(ref copyright, value);
        }

        public string Author
        {
            get => author;
            set => SetProperty(ref author, value);
        }

        public string BuildDate
        {
            get => buildDate;
            set => SetProperty(ref buildDate, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }
    }
}
