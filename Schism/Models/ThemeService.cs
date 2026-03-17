using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Schism.Models
{
    public class ThemeService
    {

        private bool _isDarkMode;
        private Brush _main;
        private Brush _accent;
        private Brush _accent2;
        private Brush _accent3;
        private Brush _Text;

        public bool IsDarkMode
        {
            get { return _isDarkMode; }
            set
            {
                _isDarkMode = value;
                UpdateTheme();
            }
        }

        public Brush Main
        {
            get { return _main; }
        }

        public Brush Accent
        {
            get { return _accent; }
        }

        public Brush Accent2 
        {
            get { return _accent2; }
        }

        public Brush Accent3 
        {
            get { return _accent3; }
        }
        public Brush Text 
        {
            get { return _Text; }
        }

        public ThemeService() {

            _isDarkMode = true; // Default to dark mode
            UpdateTheme();

        }

        private void UpdateTheme()
        {
            if(_isDarkMode)
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
                _accent = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120));
                _accent2 = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150));
                _accent3 = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
                _Text = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            }
            else
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                _accent = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
                _accent2 = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
                _accent3 = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
                _Text = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
            }
        }
    }
}
