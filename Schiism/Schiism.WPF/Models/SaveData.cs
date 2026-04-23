// <copyright file="SaveData.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schism.Models
{
    using System.Runtime.Intrinsics.X86;

    public class SaveData
    {
        // private variables
        private byte saveLength;
        private string saveStartAddress;
        private byte saveDeviceID;
        private string saveDataType;
        private string saveNumericBase;
        private string saveDataSize;
        private string saveEndian;
        private bool saveASCIIEnable;
        private string saveAddressConv;

        // Constructor
        public SaveData(byte sL, string sSA, byte sDID, string sDT, string sNB, string sDS, string sE, bool sAE, string sAC)
        {
            this.saveLength = sL;
            this.saveStartAddress = sSA;
            this.saveDeviceID = sDID;
            this.saveDataType = sDT;
            this.saveNumericBase = sNB;
            this.saveDataSize = sDS;
            this.saveEndian = sE;
            this.saveASCIIEnable = sAE;
            this.saveAddressConv = sAC;
        }

        // Empty Constructor (for loading data)
        public SaveData()
        {
            this.saveLength = 0;
            this.saveStartAddress = string.Empty;
            this.saveDeviceID = 0;
            this.saveDataType = string.Empty;
            this.saveNumericBase = string.Empty;
            this.saveDataSize = string.Empty;
            this.saveEndian = string.Empty;
            this.saveASCIIEnable = false;
            this.saveAddressConv = string.Empty;
        }

        // Simple getters and setters for each variable
        public byte SaveLength { get => this.saveLength; set => this.saveLength = value; }

        public string SaveStartAddress { get => this.saveStartAddress; set => this.saveStartAddress = value; }

        public byte SaveDeviceId { get => this.saveDeviceID; set => this.saveDeviceID = value; }

        public string SaveDataType { get => this.saveDataType; set => this.saveDataType = value; }

        public string SaveNumericBase { get => this.saveNumericBase; set => this.saveNumericBase = value; }

        public string SaveDataSize { get => this.saveDataSize; set => this.saveDataSize = value; }

        public string SaveEndian { get => this.saveEndian; set => this.saveEndian = value; }

        public bool SaveAsciiEnable { get => this.saveASCIIEnable; set => this.saveASCIIEnable = value; }

        public string SaveAddressConv { get => this.saveAddressConv; set => this.saveAddressConv = value; }
    }
}
