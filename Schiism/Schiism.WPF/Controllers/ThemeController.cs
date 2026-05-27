// <copyright file="ThemeService.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Controllers
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;

    public class ThemeController : INotifyPropertyChanged
    {
        // dropdown contents (never change)
        private readonly ObservableCollection<string> availableThemes;

        // Private variables
        private Brush main;
        private Brush accent1;
        private Brush accent2;
        private Brush accent3;
        private Brush accent4;
        private Brush textColor;
        private Brush errorColor;

        // Dropdown selected variables
        private string selectedTheme;

        // Constructor
        public ThemeController()
        {
            // Initialize available themes and set default theme
            availableThemes = new ObservableCollection<string> { "Dark", "Light" };
            selectedTheme = availableThemes.First();

            // Initialize theme colors based on the default selected theme (dark)
            main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
            accent1 = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
            accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
            accent3 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
            accent4 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            textColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            errorColor = new SolidColorBrush(Color.FromArgb(255, 120, 0, 0));

            // Notify the UI of the initial theme selection
            OnPropertyChanged(nameof(SelectedTheme));
        }

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;

        // Singleton instance
        public static ThemeController Instance { get; } = new();

        // Public properties with getters and setters that notify the UI of changes
        public string SelectedTheme
        {
            get => selectedTheme;
            set
            {
                selectedTheme = value;
                UpdateTheme();
            }
        }

        public Brush Main
        {
            get => main;
            set
            {
                if (main != value)
                {
                    main = value;
                }
            }
        }

        public Brush Accent1
        {
            get => accent1;
            set
            {
                if (accent1 != value)
                {
                    accent1 = value;
                }
            }
        }

        public Brush Accent2
        {
            get => accent2;
            set
            {
                if (accent2 != value)
                {
                    accent2 = value;
                }
            }
        }

        public Brush Accent3
        {
            get => accent3;
            set
            {
                if (accent3 != value)
                {
                    accent3 = value;
                }
            }
        }

        public Brush Accent4
        {
            get => accent4;
            set
            {
                if (accent4 != value)
                {
                    accent4 = value;
                }
            }
        }

        public Brush TextColor
        {
            get => textColor;
            set
            {
                if (textColor != value)
                {
                    textColor = value;
                }
            }
        }

        public Brush ErrorColor
        {
            get => errorColor;
            set
            {
                if (errorColor != value)
                {
                    errorColor = value;
                }
            }
        }

        // ObservableCollection public binding
        public ObservableCollection<string> AvailableThemes => availableThemes;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Method to update theme colors based on the selected theme
        private void UpdateTheme()
        {
            if (SelectedTheme == "Dark")
            {
                main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
                accent1 = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
                accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
                accent3 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
                accent4 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                textColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                errorColor = new SolidColorBrush(Color.FromArgb(255, 120, 0, 0));
            }
            else
            {
                main = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250));
                accent1 = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                accent2 = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                accent3 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                accent4 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
                textColor = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                errorColor = new SolidColorBrush(Color.FromArgb(255, 200, 0, 0));
            }

            OnPropertyChanged(nameof(Main));
            OnPropertyChanged(nameof(Accent1));
            OnPropertyChanged(nameof(Accent2));
            OnPropertyChanged(nameof(Accent3));
            OnPropertyChanged(nameof(Accent4));
            OnPropertyChanged(nameof(TextColor));
            OnPropertyChanged(nameof(ErrorColor));
        }
    }
}
