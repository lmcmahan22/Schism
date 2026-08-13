// <copyright file="ModbusRow.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.Diagnostics;

namespace Schiism.WPF.Models
{

    public class ModbusRow : BindableBase
    {
        private string name;
        private string data;
        private bool isUpdating;

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

        public bool IsUpdating
        {
            get => isUpdating;
            set
            {
                Debug.WriteLine($"CHANGED: {GetHashCode()}");

                SetProperty(ref isUpdating, value);
            }
        }

        public ModbusRow(string name, string data, bool isUpdating)
        {
            this.name = name;
            this.data = data;
            this.isUpdating = isUpdating;
        }
    }
}
