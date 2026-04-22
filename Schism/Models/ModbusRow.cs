// <copyright file="ModbusRow.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Schism.Models
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ModbusRow : INotifyPropertyChanged
    {
        private string name;
        private string data;

        public ModbusRow(string name, string data)
        {
            this.name = name;
            this.data = data;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => this.name;
            set
            {
                if (this.name == value)
                {
                    return;
                }

                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public string Data
        {
            get => this.data;
            set
            {
                if (this.data == value)
                {
                    return;
                }

                this.data = value;
                this.OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
