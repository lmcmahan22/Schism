using System.Runtime.CompilerServices;

namespace Schism.ViewModels
{
    public class AboutViewModel: BindableBase
    {
        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // View Model properties
        private string _title = "About";
        private string _version = "Version: 1.0.0";
        private string _buildDate = "Build Date: 04/01/2026";
        private string _appName = "MODBUS TCP Client Simulator";
        private string _copyright = "©2026 Precision Valve & Automation, Inc.";
        private string _author = "Author: Liam McMahan (Product Development)";

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

        public AboutViewModel()
        {
            // Empty, since we have already established what all of our variables are equal to. Everything here is View only, but we have a ViewModel just so we can easily control these variables outside of ViewModel code.
            
        }

        // Event handler? Might not be needed anymore
        // private event EventHandler CloseRequested;

        //public DialogCloseListener RequestClose => throw new NotImplementedException();

        //public bool CanCloseDialog()
        //{
        //    return true;
        //}

        //public void OnDialogClosed()
        //{

        //}

        //public void OnDialogOpened(IDialogParameters parameters)
        //{

        //}
    }
}
