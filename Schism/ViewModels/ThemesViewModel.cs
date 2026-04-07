using Schism.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Schism.ViewModels
{
    public class ThemesViewModel : BindableBase
    {
        // INotifyPropertyChanged interface for ViewModels
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        // View Model properties
        private string _title = "Themes";

        // Service Singleton (see App.xml)
        public ThemeService TS => ThemeService.Instance; // ThemeService is a singleton, so we access the instance directly

        // Public instances of the ViewModel for control in the View
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        // Constructor
        public ThemesViewModel()
        {
            // Empty, since all references to Model data will be referenced as "TS.X" in View
        }

        // IDialogAware interface. Might not be needed!

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
