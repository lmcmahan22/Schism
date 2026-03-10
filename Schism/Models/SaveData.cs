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
        private int _saveLength = 0;
        private int _saveStartAddress = 0;
        private int _saveDeviceID = 0;
        private string _saveDataType = "";
        private string _saveNumericBase = "";
        private string _saveEndian = "";
        private bool _saveASCIIEnable;
        private string _saveADisplayType = "";

        public SaveData()
        {
            // Default constructor
        }

        public SaveData(int _sL, int _sSA, int _sDID, string _sDT,
            string _sNB, string _sE, bool _sAE,
            string _sADT)
        {
            _saveLength = _sL;
            _saveStartAddress = _sSA;
            _saveDeviceID = _sDID;
            _saveDataType = _sDT;
            _saveNumericBase = _sNB;
            _saveEndian = _sE;
            _saveASCIIEnable = _sAE;
            _saveADisplayType = _sADT;
        }

        // Getters and setters for each variable

        public int SaveLength { get => _saveLength; set => _saveLength = value; }
        public int SaveStartAddress { get => _saveStartAddress; set => _saveStartAddress = value; }
        public int SaveDeviceID { get => _saveDeviceID; set => _saveDeviceID = value; }
        public string SaveDataType { get => _saveDataType; set => _saveDataType = value; }
        public string SaveNumericBase { get => _saveNumericBase; set => _saveNumericBase = value; }
        public string SaveEndian { get => _saveEndian; set => _saveEndian = value; }
        public bool SaveASCIIEnable { get => _saveASCIIEnable; set => _saveASCIIEnable = value; }
        public string SaveADisplayType { get => _saveADisplayType; set => _saveADisplayType = value; }
    }
}
