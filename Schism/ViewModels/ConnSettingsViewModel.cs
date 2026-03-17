using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Schism.ViewModels
{
    public class ConnSettingsViewModel : BindableBase, IDialogAware
    {

        // The helper method to raise the event
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
           base.RaisePropertyChanged(propertyName);
        }

        private string _title = "Connection Settings";
        private string _ipAddress = "127.0.0.1";
        private string _tcpPort = "502";
        private int _scanRate = 1000;
        private int _timeout = 1000;
        private int _pollDelay = 10;
        private ObservableCollection<string> _addressConvention = new ObservableCollection<string>
            {
                "Register Address (starting from 0)",
                "Modbus RTU over TCP (starting from 1)"
            };
        private string _selectedAddressConvention = "";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set { SetProperty(ref _ipAddress, value); }
        }

        public string TCPPort
        {
            get => _tcpPort;
            set { SetProperty(ref _tcpPort, value); }
}

        public int ScanRate
        {
            get => _scanRate;
            set { SetProperty(ref _scanRate, value); }
        }

        public int Timeout
        {
            get => _timeout;
            set { SetProperty(ref _timeout, value); }
        }

        public int PollDelay
        {
            get => _pollDelay;
            set { SetProperty(ref _pollDelay, value); }
        }

        public ObservableCollection<string> AddressConvention
        {
            get => _addressConvention;
            set => SetProperty(ref _addressConvention, value);
        }
        public string SelectedAddressConvention
        {
            get => _selectedAddressConvention;
            set { SetProperty(ref _selectedAddressConvention, value); }
        }

        // Constructor
        public ConnSettingsViewModel()
        {
            _selectedAddressConvention = _addressConvention.FirstOrDefault();
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
