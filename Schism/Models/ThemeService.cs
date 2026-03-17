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

        public ThemeService() {

            _isDarkMode = true; // Default to dark mode
            UpdateTheme();

        }

        private void UpdateTheme()
        {
            if(_isDarkMode)
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
                _accent = new SolidColorBrush(Color.FromArgb(255, 45, 45, 45));
            }
            else
            {
                _main = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                _accent = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));
            }
        }
    }
}
