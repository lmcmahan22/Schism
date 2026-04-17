using System.Runtime.CompilerServices;

namespace Schism.ViewModels
{
    public class AboutViewModel : BindableBase
    {
        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // Private variables
        private string _title;
        private string _version;
        private string _buildDate;
        private string _appName;
        private string _copyright;
        private string _author;

        // Public instances with getters/setters
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

        // Constructor
        public AboutViewModel()
        {
            _title = "About";
            _version = "Version: 1.0.0";
            _buildDate = "Build Date: 04/20/2026";
            _appName = "MODBUS TCP Client Simulator";
            _copyright = "©2026 Precision Valve & Automation, Inc.";
            _author = "Author: Liam McMahan (Product Development)";
        }
    }
}
