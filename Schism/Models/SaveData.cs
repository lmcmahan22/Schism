using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schism.Models
{
    public class SaveData
    {
        private ushort _saveLength = 0;
        private ushort _saveStartAddress = 0;
        private byte _saveDeviceID = 0;
        private string _saveDataType = "";
        private string _saveNumericBase = "";
        private string _saveDataSize = "";
        private string _saveEndian = "";
        private bool _saveASCIIEnable;
        private string _saveADisplayType = "";
        private string _saveAddressConv = "";

        public SaveData()
        {
            // Default constructor
        }

        public SaveData(ushort _sL, ushort _sSA, byte _sDID, string _sDT,
            string _sNB, string _sDS, string _sE, bool _sAE,
            string _sADT, string _sAC)
        {
            _saveLength = _sL;
            _saveStartAddress = _sSA;
            _saveDeviceID = _sDID;
            _saveDataType = _sDT;
            _saveNumericBase = _sNB;
            _saveDataSize = _sDS;
            _saveEndian = _sE;
            _saveASCIIEnable = _sAE;
            _saveADisplayType = _sADT;
            _saveAddressConv = _sAC;
        }

        // Getters and setters for each variable

        public ushort SaveLength { get => _saveLength; set => _saveLength = value; }
        public ushort SaveStartAddress { get => _saveStartAddress; set => _saveStartAddress = value; }
        public byte SaveDeviceId { get => _saveDeviceID; set => _saveDeviceID = value; }
        public string SaveDataType { get => _saveDataType; set => _saveDataType = value; }
        public string SaveNumericBase { get => _saveNumericBase; set => _saveNumericBase = value; }
        public string SaveDataSize { get => _saveDataSize; set => _saveDataSize = value; }
        public string SaveEndian { get => _saveEndian; set => _saveEndian = value; }
        public bool SaveAsciiEnable { get => _saveASCIIEnable; set => _saveASCIIEnable = value; }
        public string SaveAsciiDisplayType { get => _saveADisplayType; set => _saveADisplayType = value; }
        public string SaveAddressConv { get => _saveAddressConv; set => _saveAddressConv = value; }
    }
}
