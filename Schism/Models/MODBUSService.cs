using NModbus;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace Schism.Models
{

    public class MODBUSService : INotifyPropertyChanged
    {

        // Singleton instance
        private static readonly Lazy<MODBUSService> _instance = new(() => new MODBUSService());
        public static MODBUSService Instance => _instance.Value;

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Private variables
        private string _ipAddr;
        private int _tcpPort;
        private int _scanRate;
        private int _tcpTimeout;
        private int _numPolls;
        private int _numOKs;
        private int _numErrors;
        private int _numTx;
        private int _numRx;
        private int _numRequests;
        private int _numResponses;
        private byte _deviceId;
        private ushort _dataLength;
        private ushort _startAddress; // Don't worry about leading zeros, Radzio just interprets a value without leading zeros as having leading zeroes (i.e. output coil range). Don't worry aboout "Global Data" either, since again Radzio doesn't bother.
        private bool _asciiEnable;
        private bool _isConnected;
        private string _selectedDataType;
        private string _selectedNumericBase;
        private string _selectedEndian;
        private string _selectedAsciiDisplayType;

        // dropdown contents (never change)
        private readonly ObservableCollection<string> _dataTypes = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private readonly ObservableCollection<string> _numericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" };
        private readonly ObservableCollection<string> _endians = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Swap Words)", "Big Endian (Swap Bytes)" };
        private readonly ObservableCollection<string> _asciiDisplayTypes = new ObservableCollection<string> { "1 Char/Reg", "2 Char/Reg", "2 Char/Reg SW." };

        // ModbusData Collection
        private ObservableCollection<StringWrapper> _modbusData = new ObservableCollection<StringWrapper>();

        // Properties for connection settings
        public string IPAddr
        {
            get => _ipAddr;
            set
            {
                if(_ipAddr != value)
                {
                    _ipAddr = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TCPPort
        {
            get => _tcpPort;
            set
            {
                if (_tcpPort != value)
                {
                    _tcpPort = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ScanRate
        {
            get => _scanRate;
            set
            {
                if (_scanRate != value)
                {
                    _scanRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TCPTimeout
        {
            get => _tcpTimeout;
            set
            {
                if (_tcpTimeout != value)
                {
                    _tcpPort = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumPolls
        {
            get => _numPolls;
            set
            {
                if (_numPolls != value)
                {
                    _numPolls = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumOKs
        {
            get => _numOKs;
            set
            {
                if (_numOKs != value)
                {
                    _numOKs = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumErrors
        {
            get => _numErrors;
            set
            {
                if (_numErrors != value)
                {
                    _numErrors = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumTX
        {
            get => _numTx;
            set
            {
                if (_numTx != value)
                {
                    _numTx = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumRX
        {
            get => _numRx;
            set
            {
                if (_numRx != value)
                {
                    _numRx = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumRequests
        {
            get => _numRequests;
            set
            {
                if (_numRequests != value)
                {
                    _numRequests = value;
                    OnPropertyChanged();
                }
            }
        }

        public int NumResponses
        {
            get => _numResponses;
            set
            {
                if (_numResponses != value)
                {
                    _numResponses = value;
                    OnPropertyChanged();
                }
            }
        }

        public byte DeviceId
        {
            get { return _deviceId; }
            set
            {
                // Min and Max boundaries on Device ID, according to MODBUS documentation
                byte clamped = Math.Clamp(value, (byte)1, (byte)247);
                if (_deviceId != clamped)
                {
                    _deviceId = clamped;
                    OnPropertyChanged();
                }
            }
        }

        public ushort DataLength
        {
            get { return _dataLength; }
            set
            {
                // Min and Max boundaries on Value relative to current StartAddress
                ushort maxLen = GetMaxLengthForStartAddress();
                ushort clampedDataLength = Math.Clamp(value, (ushort)1, maxLen);

                if (_dataLength != clampedDataLength)
                {
                    _dataLength = clampedDataLength;
                    OnPropertyChanged();
                }
            }
        }

        public ushort StartAddress
        {
            get { return _startAddress; }
            set
            {
                // Min and Max boundaries on Starting Address, according to MODBUS documentation
                ushort clampedStart = Math.Clamp(value, (ushort)0, (ushort)65535);

                if (_startAddress != clampedStart)
                {
                    _startAddress = clampedStart;
                    // When start address changes, ensure the current length does not exceed the new allowable range.
                    ushort maxLen = GetMaxLengthForStartAddress();
                    ushort clampedDataLength = Math.Clamp(_dataLength, (ushort)1, maxLen);

                    if (_dataLength != clampedDataLength)
                    {
                        _dataLength = clampedDataLength;
                        OnPropertyChanged();
                    }
                }
            }
        }

        public bool AsciiEnable
        {
            get => _asciiEnable;
            set
            {
                if (_asciiEnable != value)
                {
                    _asciiEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedDataType
        {
            get => _selectedDataType;
            set
            {
                if (_selectedDataType != value)
                {
                    _selectedDataType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedNumericBase
        {
            get => _selectedNumericBase;
            set
            {
                if (_selectedDataType != value)
                {
                    _selectedDataType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedEndian
        {
            get => _selectedEndian;
            set
            {
                if (_selectedEndian != value)
                {
                    _selectedEndian = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedAsciiDisplayType
        {
            get => _selectedAsciiDisplayType;
            set
            {
                if (_selectedAsciiDisplayType != value)
                {
                    _selectedAsciiDisplayType = value;
                    OnPropertyChanged();
                }
            }
        }

        // Make Observable Collections public. None of these need Getters/Setters, by nature of ObservableCollections
        public ObservableCollection<string> DataTypes => _dataTypes;
        public ObservableCollection<string> NumericBases => _numericBases;
        public ObservableCollection<string> Endians => _endians;
        public ObservableCollection<string> AsciiDisplayTypes => _asciiDisplayTypes;
        public ObservableCollection<StringWrapper> ModbusData => _modbusData;

        // Consutrctor
        private MODBUSService()
        {
            IPAddr = "165.165.165.11";
            TCPPort = 502;
            ScanRate = 1000;
            TCPTimeout = 5000;
            NumPolls = 0;
            NumOKs = 0;
            NumErrors = 0;
            NumTX = 0;
            NumRX = 0;
            NumRequests = 0;
            NumResponses = 0;
            DeviceId = 1;
            DataLength = 10;
            StartAddress = 0;
            AsciiEnable = false;
            SelectedDataType = DataTypes.First();
            SelectedNumericBase = NumericBases.First();
            SelectedEndian = Endians.First();
            SelectedAsciiDisplayType = DataTypes.First();
        }

        public async void Connection(){

            await Task.Run(() => MODBUSComms());
        }

        private void MODBUSComms()
        {
            try
            {
                IPAddress address = IPAddress.Parse(IPAddr);
                TcpClient masterTcpClient = new TcpClient(address.ToString(), TCPPort);
                // Create the MODBUS factory, which handles MODBUS operations
                var factory = new ModbusFactory();
                IModbusMaster modbusMaster = factory.CreateMaster(masterTcpClient);

                // Apply configurable timeout value from the connections window
                modbusMaster.Transport.ReadTimeout = TCPTimeout;
                modbusMaster.Transport.WriteTimeout = TCPTimeout;
                modbusMaster.Transport.Retries = 3;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsConnected = true;
                });

                switch (SelectedDataType)
                {
                    //{ "Coil Status", "Input Status", "Holding Registers", "Input Registers" }
                    case "Coil Status":
                        ReadCoils(masterTcpClient, modbusMaster);
                        break;
                    case "Input Status":
                        ReadInputs(masterTcpClient,modbusMaster);
                        break;
                    case "Holding Registers":
                        ReadHoldingRegs(masterTcpClient, modbusMaster);
                        break;
                    case "Input Registers":
                        ReadInputRegs(masterTcpClient, modbusMaster);
                        break;
                    default:
                        // This will never occur...
                        break;
                }
            }

            catch (Exception toe) when (toe is IOException or TimeoutException)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.IsConnected = false;
                });
                MessageBox.Show($"Application MODBUS Timeout Failure: \n Timeout period reached during all 3 connection attempts. \n");
            }

            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.IsConnected = false;
                });
                MessageBox.Show($"Unknown Application/MODBUS Failure: \n" + e.Message);
            }
        }

        private void ReadCoils(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (IsConnected)
                {
                    bool[] coils = mM.ReadCoils(0, StartAddress, DataLength);
                    ushort[] coilsConv = coils.Select(Convert.ToUInt16).ToArray();

                    var newData = new ObservableCollection<StringWrapper>();

                    for (int i = 0; i < DataLength; i++)
                        newData.Add(new StringWrapper(coilsConv[i].ToString()));

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _modbusData.Clear();

                        foreach (var item in newData)
                            _modbusData.Add(item);
                    });

                    Thread.Sleep(ScanRate);
                }
                // Does closing the app also close and stop these?
                mtc.Close();
            }
        }

        private void ReadInputs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc) {

                while (IsConnected)
                {
                    bool[] inputs = mM.ReadInputs(0, StartAddress, DataLength);
                    ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();

                    var newData = new ObservableCollection<StringWrapper>();

                    for (int i = 0; i < DataLength; i++)
                        newData.Add(new StringWrapper(inputsConv[i].ToString()));

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _modbusData.Clear();

                        foreach (var item in newData)
                            _modbusData.Add(item);
                    });

                    Thread.Sleep(ScanRate);
                }
                // Does closing the app also close and stop these?
                mtc.Close();
            }
        }

        private void ReadHoldingRegs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (IsConnected)
                {
                    ushort[] holdingRegs = mM.ReadHoldingRegisters(0, StartAddress, DataLength);

                    var newData = new ObservableCollection<StringWrapper>();

                    for (int i = 0; i < DataLength; i++)
                        newData.Add(new StringWrapper(holdingRegs[i].ToString()));

                    //NOTE: This is where you'll implement the numeric base and endian control!
                    // Possibly even the ASCII control as well.
                    // You'll need to convert the received data based on that UI selection.
                    // Each piece of data will then be converted into a string to be displayed in the UI

                    // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" }

                    switch (SelectedNumericBase)
                    {
                        case "Integer":
                            short[] holdingRegsSigned = new short[holdingRegs.Length];
                            for (int i = 0; i < DataLength; i++)
                            {
                                holdingRegsSigned[i] = (short)holdingRegs[i];
                                newData.Add(new StringWrapper(holdingRegsSigned[i].ToString()));
                            }
                            break;
                        case "Hexadecimal":
                            for (int i = 0; i < DataLength; i++)
                            {
                                // the added "X" in the ToString parentheses does the conversion for us, since hex can't be parsed as a new numeric variable type
                                newData.Add(new StringWrapper("0x" + holdingRegs[i].ToString("X")));
                            }
                            break;
                        case "Binary":
                            for (int i = 0; i < DataLength; i++)
                            {
                                string temp = Convert.ToString(holdingRegs[i], 2); // 2 parameter converts value to a binary string
                                string paddedTemp = temp.PadLeft(16, '0');
                                string formattedTemp = Regex.Replace(paddedTemp, ".{4}", "$0 ").Trim();
                                newData.Add(new StringWrapper(formattedTemp));
                            }
                            break;
                        // decimal
                        default:
                            for (int i = 0; i < DataLength; i++)
                                newData.Add(new StringWrapper(holdingRegs[i].ToString()));
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _modbusData.Clear();

                        foreach (var item in newData)
                            _modbusData.Add(item);
                    });

                    Thread.Sleep(ScanRate);
                }
                // Does closing the app also close and stop these?
                mtc.Close();
            }
        }

        private void ReadInputRegs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (IsConnected)
                {
                    ushort[] inputRegs = mM.ReadInputRegisters(0, StartAddress, DataLength);

                    var newData = new ObservableCollection<StringWrapper>();

                    for (int i = 0; i < DataLength; i++)
                        newData.Add(new StringWrapper(inputRegs[i].ToString()));

                    //NOTE: This is where you'll implement the numeric base and endian control!
                    // Possibly even the ASCII control as well.
                    // You'll need to convert the received data based on that UI selection.
                    // Each piece of data will then be converted into a string to be displayed in the UI

                    // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" }

                    switch (SelectedNumericBase)
                    {
                        case "Integer":
                            short[] inputRegsSigned = new short[inputRegs.Length];
                            for (int i = 0; i < DataLength; i++)
                            {
                                inputRegsSigned[i] = (short)inputRegs[i];
                                newData.Add(new StringWrapper(inputRegsSigned[i].ToString()));
                            }
                            break;
                        case "Hexadecimal":
                            for (int i = 0; i < DataLength; i++)
                            {
                                // the added "X" in the ToString parentheses does the conversion for us, since hex can't be parsed as a new numeric variable type
                                newData.Add(new StringWrapper("0x"+inputRegs[i].ToString("X")));
                            }
                            break;
                        case "Binary":
                            for (int i = 0; i < DataLength; i++)
                            {
                                string temp = Convert.ToString(inputRegs[i], 2); // 2 parameter converts value to a binary string
                                string paddedTemp = temp.PadLeft(16, '0');
                                string formattedTemp = Regex.Replace(paddedTemp, ".{4}", "$0 ").Trim();
                                newData.Add(new StringWrapper(formattedTemp));
                            }
                            break;
                        // decimal
                        default:
                            for (int i = 0; i < DataLength; i++)
                                newData.Add(new StringWrapper(inputRegs[i].ToString()));
                            break;
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _modbusData.Clear();

                        foreach (var item in newData)
                            _modbusData.Add(item);
                    });

                    Thread.Sleep(ScanRate);
                }
                // Does closing the app also close and stop these?
                mtc.Close();
            }
        }

        private ushort GetMaxLengthForStartAddress()
        {
            ushort cap = (ushort)(65535 - _startAddress); // inclusive cap
            return Math.Min((ushort)120, cap);
        }
    }
}
