namespace Schiism.Core.Domain
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ModbusDeviceConfig
    {
        // Private wariables
        private string ipAddr;
        private ushort tcpPort;
        private ushort scanRate;
        private ushort tcpTimeout;
        private byte deviceId;
        private ushort dataLength;
        private string startAddress;
        private bool asciiEnable;
        private string errMess;
        private string selectedDataType;
        private string selectedDataSize;
        private string selectedNumericBase;
        private string selectedEndian;

        // Consutrctor
        private ModbusDeviceConfig()
        {
            this.ipAddr = "192.168.100.020";
            this.tcpPort = 502;
            this.scanRate = 500;
            this.tcpTimeout = 5000;
            this.deviceId = 1;
            this.dataLength = 10;
            this.startAddress = "0";
            this.asciiEnable = false;
            this.errMess = string.Empty;
            this.selectedDataType = "Coil Status";
            this.selectedDataSize = "16-Bit";
            this.selectedNumericBase = "Decimal";
            this.selectedEndian = "Big Endian";
        }

        // Accessors

        public string IPAddress
        {
            get => this.ipAddr;
            set => this.ipAddr = value;
        }

        public ushort Port
        {
            get => this.tcpPort;
            set => this.tcpPort = value;
        }

        public byte DeviceId
        {
            get => this.deviceId;
            set => this.deviceId = value;
        }

        public string StartAddress
        {
            get => this.startAddress;
            set => this.startAddress = value;
        }

        public ushort DataLength
        {
            get => this.dataLength;
            set => this.dataLength = value;
        }

        public ushort ScanRateMs
        {
            get => this.scanRate;
            set => this.scanRate = value;
        }

        public ushort TimeoutMs
        {
            get => this.tcpTimeout;
            set => this.tcpTimeout = value;
        }

        public string SelectedDataType
        {
            get => this.selectedDataType;
            set => this.selectedDataType = value;
        }

        public string SelectedDataSize
        {
            get => this.selectedDataSize;
            set => this.selectedDataSize = value;
        }

        public string SelectedNumericBase
        {
            get => this.selectedNumericBase;
            set => this.selectedNumericBase = value;
        }

        public string SelectedEndian
        {
            get => this.selectedEndian;
            set => this.selectedEndian = value;
        }

        public bool AsciiEnable
        {
            get => this.asciiEnable;
            set => this.asciiEnable = value;
        }
    }
}
