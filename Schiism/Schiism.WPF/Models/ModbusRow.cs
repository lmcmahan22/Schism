// <copyright file="ModbusRow.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Models
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
                
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
