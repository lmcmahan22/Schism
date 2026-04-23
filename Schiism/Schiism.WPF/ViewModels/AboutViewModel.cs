// <copyright file="AboutViewModel.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.ViewModels
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
            this.title = "About";
            this.version = "Version: 1.0.0";
            this.buildDate = "Build Date: 04/20/2026";
            this.appName = "MODBUS TCP Client Simulator";
            this.copyright = "©2026 Precision Valve & Automation, Inc.";
            this.author = "Author: Liam McMahan (Product Development)";
        }

        // Public instances with getters/setters
        public string Title
        {
            get { return this.title; }
            set { this.SetProperty(ref this.title, value); }
        }

        public string Version
        {
            get => this.version;
            set => this.SetProperty(ref this.version, value);
        }

        public string AppName
        {
            get => this.appName;
            set => this.SetProperty(ref this.appName, value);
        }

        public string Copyright
        {
            get => this.copyright;
            set => this.SetProperty(ref this.copyright, value);
        }

        public string Author
        {
            get => this.author;
            set => this.SetProperty(ref this.author, value);
        }

        public string BuildDate
        {
            get => this.buildDate;
            set => this.SetProperty(ref this.buildDate, value);
        }

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.RaisePropertyChanged(propertyName);
        }
    }
}
