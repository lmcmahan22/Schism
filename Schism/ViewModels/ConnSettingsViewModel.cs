using Schism.Services;
using System.Runtime.CompilerServices;

namespace Schism.ViewModels
{
    public class ConnSettingsViewModel : BindableBase
    {

        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // Private variable
        private string _title;

        // Service Singleton that gets passed up to View
        public MODBUSService MS => MODBUSService.Instance;

        // Public instance with getter/setter
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // Constructor
        public ConnSettingsViewModel()
        {
            _title = "Connection Settings";
        }
    }
}
