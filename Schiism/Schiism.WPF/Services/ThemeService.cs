// <copyright file="ThemeService.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Services
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;

    public class ThemeService : INotifyPropertyChanged
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
        public ThemeService()
        {
            // Initialize available themes and set default theme
            this.availableThemes = new ObservableCollection<string> { "Dark", "Light" };
            this.selectedTheme = this.availableThemes.First();

            // Initialize theme colors based on the default selected theme (dark)
            this.main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
            this.accent1 = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
            this.accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
            this.accent3 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
            this.accent4 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            this.textColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            this.errorColor = new SolidColorBrush(Color.FromArgb(255, 120, 0, 0));

            // Notify the UI of the initial theme selection
            this.OnPropertyChanged(nameof(this.SelectedTheme));
        }

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;

        // Singleton instance
        public static ThemeService Instance { get; } = new();

        // Public properties with getters and setters that notify the UI of changes
        public string SelectedTheme
        {
            get => this.selectedTheme;
            set
            {
                this.selectedTheme = value;
                this.UpdateTheme();
            }
        }

        public Brush Main
        {
            get => this.main;
            set
            {
                if (this.main != value)
                {
                    this.main = value;
                    
                }
            }
        }

        public Brush Accent1
        {
            get => this.accent1;
            set
            {
                if (this.accent1 != value)
                {
                    this.accent1 = value;
                    
                }
            }
        }

        public Brush Accent2
        {
            get => this.accent2;
            set
            {
                if (this.accent2 != value)
                {
                    this.accent2 = value;
                    
                }
            }
        }

        public Brush Accent3
        {
            get => this.accent3;
            set
            {
                if (this.accent3 != value)
                {
                    this.accent3 = value;
                    
                }
            }
        }

        public Brush Accent4
        {
            get => this.accent4;
            set
            {
                if (this.accent4 != value)
                {
                    this.accent4 = value;
                    
                }
            }
        }

        public Brush TextColor
        {
            get => this.textColor;
            set
            {
                if (this.textColor != value)
                {
                    this.textColor = value;
                    
                }
            }
        }

        public Brush ErrorColor
        {
            get => this.errorColor;
            set
            {
                if (this.errorColor != value)
                {
                    this.errorColor = value;
                    
                }
            }
        }

        // ObservableCollection public binding
        public ObservableCollection<string> AvailableThemes => this.availableThemes;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Method to update theme colors based on the selected theme
        private void UpdateTheme()
        {
            if (this.SelectedTheme == "Dark")
            {
                this.main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
                this.accent1 = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
                this.accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
                this.accent3 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
                this.accent4 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                this.textColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                this.errorColor = new SolidColorBrush(Color.FromArgb(255, 120, 0, 0));
            }
            else
            {
                this.main = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250));
                this.accent1 = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                this.accent2 = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                this.accent3 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                this.accent4 = new SolidColorBrush(Color.FromArgb(255, 175, 175, 175));
                this.textColor = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                this.errorColor = new SolidColorBrush(Color.FromArgb(255, 200, 0, 0));
            }

            this.OnPropertyChanged(nameof(this.Main));
            this.OnPropertyChanged(nameof(this.Accent1));
            this.OnPropertyChanged(nameof(this.Accent2));
            this.OnPropertyChanged(nameof(this.Accent3));
            this.OnPropertyChanged(nameof(this.Accent4));
            this.OnPropertyChanged(nameof(this.TextColor));
            this.OnPropertyChanged(nameof(this.ErrorColor));
        }
    }
}
