// <copyright file="ModbusRow.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Models
{

    public class ModbusRow : BindableBase
    {
        private string name;
        private string data;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string Data
        {
            get => data;
            set => SetProperty(ref data, value);
        }

        public ModbusRow(string name, string data)
        {
            this.name = name;
            this.data = data;
        }
    }
}
