using Schism.Models;
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

        private readonly MODBUSService _MS = MODBUSService.Instance; // MODBUSService is a singleton, so we access the instance directly

        // The helper method to raise the event
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
           base.RaisePropertyChanged(propertyName);
        }

        private string _title = "Connection Settings";
        
        private ObservableCollection<string> _addressConvention = new ObservableCollection<string>
            {
                "Register Address (starting from 0)",
                "Modbus RTU over TCP (starting from 1)"
            };
        private string _selectedAddressConvention = "";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string IPAddress
        {
            get => _MS.IpAddress;
            set
            {
                if (_MS.IpAddress != value)
                {
                    _MS.IpAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TCPPort
        {
            get => _MS.TCPPort;
            set
            {
                if (_MS.TCPPort != value)
                {
                    _MS.TCPPort = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ScanRate
        {
            get => _MS.ScanRate;
            set
            {
                if (_MS.ScanRate != value)
                {
                    _MS.ScanRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Timeout
        {
            get => _MS.Timeout;
            set
            {
                if (_MS.Timeout != value)
                {
                    _MS.Timeout = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PollDelay
        {
            get => _MS.PollDelay;
            set
            {
                if (_MS.PollDelay != value)
                {
                    _MS.PollDelay = value;
                    OnPropertyChanged();
                }
            }
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
