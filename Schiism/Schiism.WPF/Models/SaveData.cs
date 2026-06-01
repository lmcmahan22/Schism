// <copyright file="SaveData.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.WPF.Models
{
    using Schiism.Core.Enums;
    using Schiism.WPF.Models.Enums;

    public class SaveData
    {
        // private variables
        private ushort saveStartAddress;
        private byte saveDeviceID;
        private PollType savePollType;
        private NumericBase saveNumericBase;
        private DataSize saveDataSize;
        private Endian saveEndian;
        private bool saveASCIIEnable;
        private AddressConvention saveAddressConv;

        // Constructor
        public SaveData(ushort sSA, byte sDID, PollType sPT, NumericBase sNB, DataSize sDS, Endian sE, bool sAE, AddressConvention sAC)
        {
            saveStartAddress = sSA;
            saveDeviceID = sDID;
            savePollType = sPT;
            saveNumericBase = sNB;
            saveDataSize = sDS;
            saveEndian = sE;
            saveASCIIEnable = sAE;
            saveAddressConv = sAC;
        }

        // Empty Constructor (for loading data)
        public SaveData()
        {
            saveStartAddress = 0;
            saveDeviceID = 0;
            savePollType = PollType.CoilStatus;
            saveNumericBase = NumericBase.Decimal;
            saveDataSize = DataSize.Bit16;
            saveEndian = Endian.BigEndian;
            saveASCIIEnable = false;
            saveAddressConv = AddressConvention.RegisterAddress;
        }

        // Simple getters and setters for each variable
        public ushort SaveStartAddress { get => saveStartAddress; set => saveStartAddress = value; }

        public byte SaveDeviceId { get => saveDeviceID; set => saveDeviceID = value; }

        public PollType SavePollType { get => savePollType; set => savePollType = value; }

        public NumericBase SaveNumericBase { get => saveNumericBase; set => saveNumericBase = value; }

        public DataSize SaveDataSize { get => saveDataSize; set => saveDataSize = value; }

        public Endian SaveEndian { get => saveEndian; set => saveEndian = value; }

        public bool SaveAsciiEnable { get => saveASCIIEnable; set => saveASCIIEnable = value; }

        public AddressConvention SaveAddressConv { get => saveAddressConv; set => saveAddressConv = value; }
    }
}
