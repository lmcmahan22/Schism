// <copyright file="ModbusRow.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.Diagnostics;

namespace Schiism.WPF.Models
{

    public class ModbusRow : BindableBase
    {
        private ushort address;
        private string name;
        private string data;
        private bool isUpdating;
        private string editData = string.Empty;

        //public event EventHandler? UserValueChanged;

        public ushort Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        // Private set in accordance to what is initiating the change (user vs modbus update)
        public string Data
        {
            get => data;
            private set => SetProperty(ref data, value);
        }

        public void SetFromModbus(string value)
        {
            Data = value;
            EditData = value;
        }

        //public void SetFromUser(string value)
        //{
        //    Data = value;
        //    EditData = value;

        //    UserValueChanged?.Invoke(this, EventArgs.Empty);
        //}

        public bool IsUpdating
        {
            get => isUpdating;
            set
            {
                Debug.WriteLine($"CHANGED: {GetHashCode()}");

                SetProperty(ref isUpdating, value);
            }
        }

        public string EditData
        {
            get => editData;
            set => SetProperty(ref editData, value);
        }

        // public DelegateCommand CommitEditCommand { get; }

        public ModbusRow(ushort address, string name, string data, bool isUpdating)
        {
            this.address = address;
            this.name = name;
            this.SetFromModbus(data);
            this.isUpdating = isUpdating;

            //this.CommitEditCommand = new DelegateCommand(CommitEdit);
        }

        //private void CommitEdit()
        //{
        //    if (EditData == Data)
        //    {
        //        return;
        //    }

        //    SetFromUser(EditData);
        //}
    }
}
