using Prism.Mvvm;

namespace Schism.ViewModels
{
    // The MainWindowViewModel class is a view model for the main window of the application.
    // It inherits from BindableBase, which provides support for property change notifications, allowing the view to update when properties in the view model change.
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Schism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
