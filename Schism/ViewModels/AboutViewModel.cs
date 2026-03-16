using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Schism.ViewModels
{
    public class AboutViewModel:BindableBase, IDialogAware
    {
        private string _title = "About";
        private string _version;
        private string _appName;
        private string _buildDate;
        private string _copyright;
        private string _author;

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        public string Copyright
        {
            get => _copyright;
            set => SetProperty(ref _copyright, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public string BuildDate
        {
            get => _buildDate;
            set => SetProperty(ref _buildDate, value);
        }

        private event EventHandler CloseRequested;

        public AboutViewModel()
        {
            // Initialize properties with default values
            _version = "Version: 1.0.0";
            _buildDate = "Build Date: 04/01/2026";
            _appName = "MODBUS TCP Client Simulator";
            _copyright = "©2026 Precision Valve & Automation, Inc.";
            _author = "Author: Liam McMahan (Product Development)";
        }

        public DialogCloseListener RequestClose => throw new NotImplementedException();

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            
        }
    }
}
