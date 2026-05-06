// <copyright file="ModbusRow.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Models
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
            get => name;
            set
            {
                if (name == value)
                {
                    return;
                }

                name = value;
                
            }
        }

        public string Data
        {
            get => data;
            set
            {
                if (data == value)
                {
                    return;
                }

                data = value;
                
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
