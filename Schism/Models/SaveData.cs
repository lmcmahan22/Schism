

namespace Schism.Models
{
    public class SaveData
    {

        // private variables
        private ushort _saveLength;
        private string _saveStartAddress;
        private byte _saveDeviceID;
        private string _saveDataType;
        private string _saveNumericBase;
        private string _saveDataSize;
        private string _saveEndian;
        private bool _saveASCIIEnable;
        private string _saveAddressConv;

        // Constructor
        public SaveData(ushort _sL, string _sSA, byte _sDID, string _sDT,
            string _sNB, string _sDS, string _sE, bool _sAE, string _sAC)
        {
            _saveLength = _sL;
            _saveStartAddress = _sSA;
            _saveDeviceID = _sDID;
            _saveDataType = _sDT;
            _saveNumericBase = _sNB;
            _saveDataSize = _sDS;
            _saveEndian = _sE;
            _saveASCIIEnable = _sAE;
            _saveAddressConv = _sAC;
        }

        // Empty Constructor (for loading data)
        public SaveData() { }

        // Simple getters and setters for each variable
        public ushort SaveLength { get => _saveLength; set => _saveLength = value; }
        public string SaveStartAddress { get => _saveStartAddress; set => _saveStartAddress = value; }
        public byte SaveDeviceId { get => _saveDeviceID; set => _saveDeviceID = value; }
        public string SaveDataType { get => _saveDataType; set => _saveDataType = value; }
        public string SaveNumericBase { get => _saveNumericBase; set => _saveNumericBase = value; }
        public string SaveDataSize { get => _saveDataSize; set => _saveDataSize = value; }
        public string SaveEndian { get => _saveEndian; set => _saveEndian = value; }
        public bool SaveAsciiEnable { get => _saveASCIIEnable; set => _saveASCIIEnable = value; }
        public string SaveAddressConv { get => _saveAddressConv; set => _saveAddressConv = value; }
    }
}
