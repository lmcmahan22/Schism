using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Schism.Models
{
    public class ModbusRow : INotifyPropertyChanged
    {
        private string _name;
        private string _data;

        public ModbusRow(string name, string data)
        {   
            _name = name;
            _data = data;
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Data
        {
            get => _data;
            set
            {
                if (_data == value) return;
                _data = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
