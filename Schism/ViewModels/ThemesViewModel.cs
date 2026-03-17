using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schism.ViewModels
{
    public class ThemesViewModel : BindableBase, IDialogAware
    {
        private string _title = "Themes";
        private ObservableCollection<string> _availableThemes;
        private string _selectedTheme;

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ObservableCollection<string> AvailableThemes
        {
            get { return _availableThemes; }
            set { SetProperty(ref _availableThemes, value); }
        }

        public string SelectedTheme
        {
            get { return _selectedTheme; }
            set { SetProperty(ref _selectedTheme, value); }
        }

        public ThemesViewModel()
        {
            // Initialize properties with default values
            _title = "Themes";
            _availableThemes = new ObservableCollection<string>
            {
                "Dark",
                "Light"
            };
            _selectedTheme = _availableThemes.FirstOrDefault();
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
