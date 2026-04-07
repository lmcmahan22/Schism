using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Schism.Models
{
    public class ThemeService : INotifyPropertyChanged
    {

        // Singleton instance
        public static ThemeService Instance { get; } = new();

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<string> _availableThemes = new ObservableCollection<string> { "Dark", "Light" }; // Example themes
        private string _selectedTheme;
        private Brush _main;
        private Brush _accent1;
        private Brush _accent2;
        private Brush _accent3;
        private Brush _textColor;

        // Do these need to call "OnPropertyChanged()"? I'm not so sure atm...
        public ObservableCollection<string> AvailableThemes => _availableThemes;

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                _selectedTheme = value;
                UpdateTheme();
            }
        }

        public Brush Main
        {
            get => _main;
            set
            {
                if (_main != value)
                {
                    _main = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush Accent1
        {
            get => _accent1;
            set
            {
                if (_accent1 != value)
                {
                    _accent1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush Accent2 
        {
            get => _accent2;
            set
            {
                if (_accent2 != value)
                {
                    _accent2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public Brush Accent3 
        {
            get => _accent3;
            set
            {
                if (_accent3 != value)
                {
                    _accent3 = value;
                    OnPropertyChanged();
                }
            }
        }
        public Brush TextColor 
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public ThemeService()
        {

            _selectedTheme = AvailableThemes.First();
            OnPropertyChanged(nameof(SelectedTheme));
            UpdateTheme(); // First call to set the initial theme colors
        }

        private void UpdateTheme()
        {
            if(SelectedTheme == "Dark")
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
                _accent1 = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
                _accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
                _accent3 = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
                _textColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            }
            else
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                _accent1 = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                _accent2 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                _accent3 = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
                _textColor = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            }

            OnPropertyChanged(nameof(Main));
            OnPropertyChanged(nameof(Accent1));
            OnPropertyChanged(nameof(Accent2));
            OnPropertyChanged(nameof(Accent3));
            OnPropertyChanged(nameof(TextColor));
        }
    }
}
