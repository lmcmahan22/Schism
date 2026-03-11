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
        private string _version;
        private string _appName;
        private string _buildDate;
        private string _appCopyright;
        private string _appAuthor;

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

        public string AppCopyright
        {
            get => _appCopyright;
            set => SetProperty(ref _appCopyright, value);
        }

        public string AppAuthor
        {
            get => _appAuthor;
            set => SetProperty(ref _appAuthor, value);
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
            _appName = "PVA MODBUS TCP Client Simulator";
            _appCopyright = "©2026 Precision Valve & Automation, Inc.";
            _appAuthor = "Author: Liam McMahan (Product Development)";
        }

        private DelegateCommand<Window> _closeButtonClick;
        public DelegateCommand<Window> CloseButtonClick =>
            _closeButtonClick ?? (_closeButtonClick = new DelegateCommand<Window>(ExecuteCloseButtonClick));

        public DialogCloseListener RequestClose => throw new NotImplementedException();

        private void ExecuteCloseButtonClick(Window? w)
        {
            w.Close();
        }

        public bool CanCloseDialog()
        {
            throw new NotImplementedException();
        }

        public void OnDialogClosed()
        {
            throw new NotImplementedException();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
