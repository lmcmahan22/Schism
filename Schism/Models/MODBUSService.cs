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
        public static MODBUSService Instance { get; } = new();

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
        private bool _attemptConnect;
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
                    _tcpTimeout = value;
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
                    OnPropertyChanged(nameof(StartAddress)); // notify StartAddress

                    ushort maxLen = GetMaxLengthForStartAddress();
                    ushort clampedDataLength = Math.Clamp(_dataLength, (ushort)1, maxLen);

                    if (_dataLength != clampedDataLength)
                    {
                        _dataLength = clampedDataLength;
                        OnPropertyChanged(nameof(DataLength)); // notify DataLength
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

        public bool AttemptConnect
        {
            get => _attemptConnect;
            set
            {
                if (_attemptConnect != value)
                {
                    _attemptConnect = value;
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
                if (_selectedNumericBase != value)
                {
                    _selectedNumericBase = value;
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

        // ModbusData ObservableCollection, which will have its data manipulated by this class.
        // This is the collection that the UI will bind to in order to display the received data. Whenever this collection is updated, the UI will automatically reflect those changes.
        public ObservableCollection<StringWrapper> ModbusData => _modbusData;

        // Consutrctor
        private MODBUSService()
        {
            _ipAddr = "165.165.165.11";
            _tcpPort = 502;
            _scanRate = 1000;
            _tcpTimeout = 5000;
            _numPolls = 0;
            _numOKs = 0;
            _numErrors = 0;
            _numTx = 0;
            _numRx = 0;
            _numRequests = 0;
            _numResponses = 0;
            _deviceId = 1;
            _dataLength = 10;
            _startAddress = 0;
            _asciiEnable = false;
            _selectedDataType = DataTypes.First();
            _selectedNumericBase = NumericBases.First();
            _selectedEndian = Endians.First();
            _selectedAsciiDisplayType = AsciiDisplayTypes.First();
        }

        public async void Connection(){

            _attemptConnect = true;
            OnPropertyChanged(nameof(AttemptConnect));

            await Task.Run(() => MODBUSComms());
        }

        private void MODBUSComms()
        {
            while (_attemptConnect)
            {
                try
                {
                    IPAddress address = IPAddress.Parse(_ipAddr);
                    TcpClient masterTcpClient = new TcpClient(address.ToString(), _tcpPort);
                    // Create the MODBUS factory, which handles MODBUS operations
                    var factory = new ModbusFactory();
                    IModbusMaster modbusMaster = factory.CreateMaster(masterTcpClient);

                    // Apply configurable timeout value from the connections window
                    modbusMaster.Transport.ReadTimeout = _tcpTimeout;
                    modbusMaster.Transport.WriteTimeout = _tcpTimeout;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _isConnected = true;
                        OnPropertyChanged(nameof(IsConnected));
                    });

                    switch (_selectedDataType)
                    {
                        //{ "Coil Status", "Input Status", "Holding Registers", "Input Registers" }
                        case "Coil Status":
                            ReadCoils(masterTcpClient, modbusMaster);
                            break;
                        case "Input Status":
                            ReadInputs(masterTcpClient, modbusMaster);
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

                catch (SlaveException se)
                {
                    // Handle slave exceptions (e.g., illegal function, illegal data address, etc.)
                    FailedPoll(); // turns isConnected to false
                    _attemptConnect = false; // stop further connection attempts if a slave exception occurs
                }

                catch (Exception e)
                {
                    FailedPoll(); // turns isConnected to false
                }
            }
        }

        private void ReadCoils(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (_isConnected && _attemptConnect)
                {
                    try
                    {
                        bool[] coils = mM.ReadCoils(0, _startAddress, _dataLength);
                        ushort[] coilsConv = coils.Select(Convert.ToUInt16).ToArray();

                        var newData = new ObservableCollection<StringWrapper>();

                        for (int i = 0; i < _dataLength; i++)
                            newData.Add(new StringWrapper(coilsConv[i].ToString()));

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                        SuccessfulPoll();
                        Thread.Sleep(_scanRate);
                    }
                    catch (Exception e)
                    {
                        FailedPoll(); // turns isConnected to false
                    }
                }
                // Does closing the app also close and stop these?
                mtc.Close();
                _isConnected = false;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private void ReadInputs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc) {

                while (_isConnected && _attemptConnect)
                {
                    try {
                        bool[] inputs = mM.ReadInputs(0, _startAddress, _dataLength);
                        ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();

                        var newData = new ObservableCollection<StringWrapper>();

                        for (int i = 0; i < _dataLength; i++)
                            newData.Add(new StringWrapper(inputsConv[i].ToString()));

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                        SuccessfulPoll();
                        Thread.Sleep(_scanRate);
                    }
                    catch (Exception e)
                    {
                        FailedPoll(); // turns isConnected to false
                    }
                }
                // Does closing the app also close and stop these?
                mtc.Close();
                _isConnected = false;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private void ReadHoldingRegs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (_isConnected && _attemptConnect)
                {
                    try {
                        ushort[] holdingRegs = mM.ReadHoldingRegisters(0, _startAddress, _dataLength);

                        var newData = new ObservableCollection<StringWrapper>();

                        for (int i = 0; i < _dataLength; i++)
                            newData.Add(new StringWrapper(holdingRegs[i].ToString()));

                        //NOTE: This is where you'll implement the numeric base and endian control!
                        // Possibly even the ASCII control as well.
                        // You'll need to convert the received data based on that UI selection.
                        // Each piece of data will then be converted into a string to be displayed in the UI

                        // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" }

                        switch (_selectedNumericBase)
                        {
                            case "Integer":
                                short[] holdingRegsSigned = new short[holdingRegs.Length];
                                for (int i = 0; i < _dataLength; i++)
                                {
                                    holdingRegsSigned[i] = (short)holdingRegs[i];
                                    newData.Add(new StringWrapper(holdingRegsSigned[i].ToString()));
                                }
                                break;
                            case "Hexadecimal":
                                for (int i = 0; i < _dataLength; i++)
                                {
                                    // the added "X" in the ToString parentheses does the conversion for us, since hex can't be parsed as a new numeric variable type
                                    newData.Add(new StringWrapper("0x" + holdingRegs[i].ToString("X")));
                                }
                                break;
                            case "Binary":
                                for (int i = 0; i < _dataLength; i++)
                                {
                                    string temp = Convert.ToString(holdingRegs[i], 2); // 2 parameter converts value to a binary string
                                    string paddedTemp = temp.PadLeft(16, '0');
                                    string formattedTemp = Regex.Replace(paddedTemp, ".{4}", "$0 ").Trim();
                                    newData.Add(new StringWrapper(formattedTemp));
                                }
                                break;
                            // decimal
                            default:
                                for (int i = 0; i < _dataLength; i++)
                                    newData.Add(new StringWrapper(holdingRegs[i].ToString()));
                                break;
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                        SuccessfulPoll();
                        Thread.Sleep(_scanRate);
                    }
                    catch (Exception e)
                    {
                        FailedPoll(); // turns isConnected to false
                    }
                }
                // Does closing the app also close and stop these?
                mtc.Close();
                _isConnected = false;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private void ReadInputRegs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (_isConnected && _attemptConnect)
                {
                    try {
                        ushort[] inputRegs = mM.ReadInputRegisters(0, _startAddress, _dataLength);

                        var newData = new ObservableCollection<StringWrapper>();

                        for (int i = 0; i < _dataLength; i++)
                            newData.Add(new StringWrapper(inputRegs[i].ToString()));

                        //NOTE: This is where you'll implement the numeric base and endian control!
                        // Possibly even the ASCII control as well.
                        // You'll need to convert the received data based on that UI selection.
                        // Each piece of data will then be converted into a string to be displayed in the UI

                        // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" }

                        switch (_selectedNumericBase)
                        {
                            case "Integer":
                                short[] inputRegsSigned = new short[inputRegs.Length];
                                for (int i = 0; i < _dataLength; i++)
                                {
                                    inputRegsSigned[i] = (short)inputRegs[i];
                                    newData.Add(new StringWrapper(inputRegsSigned[i].ToString()));
                                }
                                break;
                            case "Hexadecimal":
                                for (int i = 0; i < _dataLength; i++)
                                {
                                    // the added "X" in the ToString parentheses does the conversion for us, since hex can't be parsed as a new numeric variable type
                                    newData.Add(new StringWrapper("0x"+inputRegs[i].ToString("X")));
                                }
                                break;
                            case "Binary":
                                for (int i = 0; i < _dataLength; i++)
                                {
                                    string temp = Convert.ToString(inputRegs[i], 2); // 2 parameter converts value to a binary string
                                    string paddedTemp = temp.PadLeft(16, '0');
                                    string formattedTemp = Regex.Replace(paddedTemp, ".{4}", "$0 ").Trim();
                                    newData.Add(new StringWrapper(formattedTemp));
                                }
                                break;
                            // decimal
                            default:
                                for (int i = 0; i < _dataLength; i++)
                                    newData.Add(new StringWrapper(inputRegs[i].ToString()));
                                break;
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                        SuccessfulPoll();
                        Thread.Sleep(_scanRate);
                    }
                    catch (Exception e)
                    {
                        FailedPoll(); // turns isConnected to false
                    }
                }
                // Does closing the app also close and stop these?
                mtc.Close();
                _isConnected = false;
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        private void SuccessfulPoll()
        {
            _numPolls++;
            _numOKs++;

            OnPropertyChanged(nameof(NumPolls));
            OnPropertyChanged(nameof(NumOKs));
        }

        private void FailedPoll()
        {
            _numPolls++;
            _numErrors++;
            _isConnected = false;

            OnPropertyChanged(nameof(NumPolls));
            OnPropertyChanged(nameof(NumErrors));
            OnPropertyChanged(nameof(IsConnected));
        }

        private ushort GetMaxLengthForStartAddress()
        {
            ushort cap = (ushort)(65535 - _startAddress); // inclusive cap
            return Math.Min((ushort)120, cap);
        }
    }
}
