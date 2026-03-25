using Schism.Models;
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

        // View Model properties
        private string _title = "Connection Settings";

        // Service Singletons (see App.xml, Themes isn't implemented yet)
        // public ThemeService TS => ThemeService.Instance; // ThemeService is a singleton, so we access the instance directly
        public MODBUSService MS => MODBUSService.Instance; // MODBUSService is a singleton, so we access the instance directly

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // Constructor
        public ConnSettingsViewModel()
        {
            // Empty, since all references to Model data will be referenced as "MS.X" in View
        }


        // IDialogAware interface. Might not be needed!

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
