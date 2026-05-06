// <copyright file="SaveData.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Models
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
            saveLength = sL;
            saveStartAddress = sSA;
            saveDeviceID = sDID;
            saveDataType = sDT;
            saveNumericBase = sNB;
            saveDataSize = sDS;
            saveEndian = sE;
            saveASCIIEnable = sAE;
            saveAddressConv = sAC;
        }

        // Empty Constructor (for loading data)
        public SaveData()
        {
            saveLength = 0;
            saveStartAddress = string.Empty;
            saveDeviceID = 0;
            saveDataType = string.Empty;
            saveNumericBase = string.Empty;
            saveDataSize = string.Empty;
            saveEndian = string.Empty;
            saveASCIIEnable = false;
            saveAddressConv = string.Empty;
        }

        // Simple getters and setters for each variable
        public byte SaveLength { get => saveLength; set => saveLength = value; }

        public string SaveStartAddress { get => saveStartAddress; set => saveStartAddress = value; }

        public byte SaveDeviceId { get => saveDeviceID; set => saveDeviceID = value; }

        public string SaveDataType { get => saveDataType; set => saveDataType = value; }

        public string SaveNumericBase { get => saveNumericBase; set => saveNumericBase = value; }

        public string SaveDataSize { get => saveDataSize; set => saveDataSize = value; }

        public string SaveEndian { get => saveEndian; set => saveEndian = value; }

        public bool SaveAsciiEnable { get => saveASCIIEnable; set => saveASCIIEnable = value; }

        public string SaveAddressConv { get => saveAddressConv; set => saveAddressConv = value; }
    }
}
