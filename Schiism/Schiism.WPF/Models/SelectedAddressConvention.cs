using Schiism.Core.Configuration.Enums;
using System.ComponentModel;

namespace Schiism.WPF.Models
{
    public class SelectedAddressConvention : INotifyPropertyChanged
    {
        private AddressConvention selected;

        public SelectedAddressConvention()
        {
            selected = AddressConvention.RegisterAddress;
        }

        public AddressConvention Selected
        {
            get { return selected; }

            set
            { 
                selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
