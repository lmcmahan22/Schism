using Schism.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Schism.ViewModels
{
    public class ThemesViewModel : BindableBase
    {
        // 
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);
        }

        private readonly ThemeService _TS = ThemeService.Instance; // ThemeService is a singleton, so we access the instance directly

        private string _title = "Themes";
        private ObservableCollection<string> _availableThemes;

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
            get { return _TS.SelectedTheme; }
            set             {
                if (_TS.SelectedTheme != value)
                {
                    _TS.SelectedTheme = value;
                    OnPropertyChanged();
                }
            }
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
        }

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
