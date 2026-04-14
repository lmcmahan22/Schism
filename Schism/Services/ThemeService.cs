using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Schism.Services
{
    public class ThemeService : BindableBase
    {

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Singleton instance
        public static ThemeService Instance { get; } = new();

        // Private variables
        private Brush _main;
        private Brush _accent1;
        private Brush _accent2;
        private Brush _accent3;
        private Brush _accent4;
        private Brush _textColor;
        private Brush _errorColor;

        // Dropdown selected variables
        private string _selectedTheme;

        // dropdown contents (never change)
        private readonly ObservableCollection<string> _availableThemes;

        // Public properties with getters and setters that notify the UI of changes
        public Brush Main
        {
            get => _main;
            set => SetProperty(ref _main, value);
        }

        public Brush Accent1
        {
            get => _accent1;
            set => SetProperty(ref _accent1, value);
        }

        public Brush Accent2
        {
            get => _accent2;
            set => SetProperty(ref _accent2, value);
        }

        public Brush Accent3
        {
            get => _accent3;
            set => SetProperty(ref _accent3, value);
        }

        public Brush Accent4
        {
            get => _accent4;
            set => SetProperty(ref _accent4, value);
        }

        public Brush TextColor
        {
            get => _textColor;
            set => SetProperty(ref _textColor, value);
        }

        public Brush ErrorColor
        {
            get => _errorColor;
            set => SetProperty(ref _errorColor, value);
        }

        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        // ObservableCollection public binding
        public ObservableCollection<string> AvailableThemes => _availableThemes;

        // Constructor
        public ThemeService()
        {
            // Initialize available themes and set default theme
            _availableThemes = new ObservableCollection<string> { "Dark", "Light" };
            _selectedTheme = _availableThemes.First();

            // Initialize theme colors based on the default selected theme
            UpdateTheme();

            // Notify the UI of the initial theme selection
            OnPropertyChanged(nameof(SelectedTheme));
        }

        // Method to update theme colors based on the selected theme
        private void UpdateTheme()
        {
            if(SelectedTheme == "Dark")
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
                _accent1 = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
                _accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
                _accent3 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
                _accent4 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                _textColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                _errorColor = new SolidColorBrush(Color.FromArgb(255, 120, 0, 0));
            }
            else
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250));
                _accent1 = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                _accent2 = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                _accent3 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                _accent4 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
                _textColor = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                _errorColor = new SolidColorBrush(Color.FromArgb(255, 200, 0, 0));
            }

            OnPropertyChanged(nameof(Main));
            OnPropertyChanged(nameof(Accent1));
            OnPropertyChanged(nameof(Accent2));
            OnPropertyChanged(nameof(Accent3));
            OnPropertyChanged(nameof(Accent4));
            OnPropertyChanged(nameof(TextColor));
        }
    }
}
