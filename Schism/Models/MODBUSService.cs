using NModbus;
using NModbus.Device;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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
        //private int _numPolls;
        private int _numOKs;
        private int _numErrors;
        //private int _sizeTX;
        //private float _tXSpd;
        //private int _sizeRX;
        //private float _rXSpd;
        private int _numRequests;
        //private double _reqSpd;
        private int _numResponses;
        //private double _respSpd;
        private byte _deviceId;
        private ushort _dataLength;
        private ushort _startAddress; // Don't worry about leading zeros, Radzio just interprets a value without leading zeros as having leading zeroes (i.e. output coil range). Don't worry aboout "Global Data" either, since again Radzio doesn't bother.
        private bool _asciiEnable;
        private bool _connectEngage;
        private bool _isConnected;
        private string _selectedDataType;
        private string _selectedNumericBase;
        private string _selectedEndian;
        private string _selectedAsciiDisplayType;
        private string _errMess;

        // dropdown contents (never change)
        private readonly ObservableCollection<string> _dataTypes = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private readonly ObservableCollection<string> _numericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "64 Bit Double" };
        private readonly ObservableCollection<string> _endians = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Byte-Swap)", "Little-Endian (Byte-Swap)" };
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

        //public int NumPolls
        //{
        //    get => _numPolls;
        //    set
        //    {
        //        if (_numPolls != value)
        //        {
        //            _numPolls = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

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

        //public int SizeTX
        //{
        //    get => _sizeTX;
        //    set
        //    {
        //        if (_sizeTX != value)
        //        {
        //            _sizeTX = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public float TXSpd
        //{
        //    get => _tXSpd;
        //    set
        //    {
        //        if (_tXSpd != value)
        //        {
        //            _tXSpd = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public int SizeRX
        //{
        //    get => _sizeRX;
        //    set
        //    {
        //        if (_sizeRX != value)
        //        {
        //            _sizeRX = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        //public float RXSpd
        //{
        //    get => _rXSpd;
        //    set
        //    {
        //        if (_rXSpd != value)
        //        {
        //            _rXSpd = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

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

        //public float ReqSpd
        //{
        //    get => _reqSpd;
        //    set
        //    {
        //        if (_reqSpd != value)
        //        {
        //            _reqSpd = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

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

        //public float RespSpd
        //{
        //    get => _respSpd;
        //    set
        //    {
        //        if (_respSpd != value)
        //        {
        //            _respSpd = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

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

        public bool ConnectEngage
        {
            get => _connectEngage;
            set
            {
                if (_connectEngage != value)
                {
                    _connectEngage = value;
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

        public string ErrMess
        {
            get => _errMess;
            set
            {
                if (_errMess != value)
                {
                    _errMess = value;
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
            _scanRate = 500;
            _tcpTimeout = 5000;
            _numRequests = 0;
            //_reqSpd = 0;
            _numResponses = 0;
            //_respSpd = 0;
            //_numPolls = 0;
            _numOKs = 0;
            _numErrors = 0;
            //_sizeTX = 0;
            //_tXSpd = 0;
            //_sizeRX = 0;
            //_rXSpd = 0;
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

            _connectEngage = true;
            OnPropertyChanged(nameof(ConnectEngage));

            await Task.Run(() => MODBUSComms());
        }

        private void MODBUSComms()
        {
            TcpClient masterTcpClient = new TcpClient();
            IPAddress address = IPAddress.Parse(_ipAddr);

            ModbusFactory factory = new ModbusFactory();
            IModbusMaster modbusMaster;

            while (_connectEngage)
            {

                // Sleep loop to make sure we don't spam connection attempts on a server that isn't ready.
                // Thread.Sleep(_scanRate);

                try
                {
                    // Increment the number of requests sent
                    RequestInc();

                    // Connection Request
                    // NOTE: if your server device is configured to respond with an error message sooner than your timeout,
                    // then you will get an error response from the server, not a timeout error from the client! Remember this difference!
                    masterTcpClient = new TcpClient(address.ToString(), _tcpPort);
                    masterTcpClient.ReceiveTimeout = _tcpTimeout;
                    masterTcpClient.SendTimeout = _tcpTimeout;

                    modbusMaster = new ModbusFactory().CreateMaster(masterTcpClient);
                    modbusMaster.Transport.ReadTimeout = _tcpTimeout;
                    modbusMaster.Transport.WriteTimeout = _tcpTimeout;
                    modbusMaster.Transport.Retries = 0; // Disable NModbus retries, since we're handling retries at a higher level with the connection loop and scan rate delay.

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _isConnected = true;
                        OnPropertyChanged(nameof(IsConnected));
                    });

                    // Hop into individual polling loops
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

                catch (Exception e)
                {
                    FailResp(e);
                }
            }
        }

        private void ReadCoils(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (_connectEngage && _isConnected)
                {
                    Thread.Sleep(_scanRate);
                    try
                    {

                        // Tally data request
                        RequestInc();

                        // Verify TCP connection
                        if (!mtc.Connected)
                        {
                            _isConnected = false;
                            OnPropertyChanged(nameof(IsConnected));
                            throw new Exception("Lost connection during coil reading.");
                        }

                        // Request data over TCP
                        bool[] coils = mM.ReadCoils(_deviceId, _startAddress, _dataLength);

                        if(coils == null || coils.Length != _dataLength)
                            throw new Exception("Received null or inadequate response for coils.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Begin transforming data into a UI friendly data collection
                        ushort[] coilsConv = coils.Select(Convert.ToUInt16).ToArray();
                        var newData = new ObservableCollection<StringWrapper>();

                        // Loop through the received data and convert each piece into a StringWrapper, which is what the UI binds to.
                        for (int i = 0; i < coilsConv.Length; i++)
                            newData.Add(new StringWrapper(coilsConv[i].ToString()));

                        // Update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                    }
                    catch (Exception e)
                    {
                        FailResp(e);
                    }
                }
            }
        }

        private void ReadInputs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc) {

                while (_connectEngage && _isConnected)
                {
                    Thread.Sleep(_scanRate);
                    try
                    {
                        // Tally data request
                        RequestInc();

                        // Verify TCP connection
                        if (!mtc.Connected)
                        {
                            _isConnected = false;
                            OnPropertyChanged(nameof(IsConnected));
                            throw new Exception("Lost connection during coil reading.");
                        }

                        // Request data over TCP
                        bool[] inputs = mM.ReadInputs(_deviceId, _startAddress, _dataLength);

                        if (inputs == null || inputs.Length != _dataLength)
                            throw new Exception("Received null or inadequate response for coils.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Begin transforming data into a UI friendly data collection
                        ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();
                        var newData = new ObservableCollection<StringWrapper>();

                        // Loop through the received data and convert each piece into a StringWrapper, which is what the UI binds to.
                        for (int i = 0; i < _dataLength; i++)
                            newData.Add(new StringWrapper(inputsConv[i].ToString()));

                        // Update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                    }
                    catch (Exception e)
                    {
                        FailResp(e);
                    }
                }
            }
        }

        private void ReadHoldingRegs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (_connectEngage && _isConnected)
                {
                    Thread.Sleep(_scanRate);
                    try
                    {
                        // Tally data request
                        RequestInc();

                        // Verify TCP connection
                        if (!mtc.Connected)
                        {
                            _isConnected = false;
                            OnPropertyChanged(nameof(IsConnected));
                            throw new Exception("Lost connection during coil reading.");
                        }

                        // Request data over TCP
                        ushort[] holdingRegs = mM.ReadHoldingRegisters(_deviceId, _startAddress, _dataLength);

                        if (holdingRegs == null || holdingRegs.Length != _dataLength)
                            throw new Exception("Received null or inadequate response for coils.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Begin transforming data into a UI friendly data collection
                        var newData = new ObservableCollection<StringWrapper>();

                        // Loop through the received data and convert each piece into a StringWrapper, which is what the UI binds to.

                        //NOTE: This is where you'll implement the numeric base and endian control!
                        // Possibly even the ASCII control as well.
                        // You'll need to convert the received data based on that UI selection.
                        // Each piece of data will then be converted into a string to be displayed in the UI

                        // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "64 Bit Double"}
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
                            case "32 Bit Float":
                                for (int i = 0; i < _dataLength; i+=2)
                                {
                                    if (i + 1 >= holdingRegs.Length)
                                        break; // Prevent out-of-range access

                                    // Combine two 16-bit registers into a 32-bit integer
                                    uint combined = ((uint)holdingRegs[i] << 16) | holdingRegs[i + 1];

                                    // Convert the combined integer to a float
                                    byte[] bytes = BitConverter.GetBytes(combined);

                                    if (_selectedEndian is "Little Endian")
                                        Array.Reverse(bytes); // Ensure correct endianness

                                    else if(_selectedEndian is "Big Endian (Byte-Swap)")
                                        bytes = new byte[] { bytes[1], bytes[0], bytes[3], bytes[2] }; // Swap the byte order for big endian byte-swap

                                    else if(_selectedEndian is "Little Endian (Byte-Swap)")
                                        bytes = new byte[] { bytes[2], bytes[3], bytes[0], bytes[1] }; // Swap the byte order for little endian byte-swap

                                    float floatValue = BitConverter.ToSingle(bytes, 0);
                                    newData.Add(new StringWrapper(floatValue.ToString()));
                                    newData.Add(new StringWrapper("")); // Empty string to offset
                                }
                                break;
                            case "64 Bit Double":
                                for (int i = 0; i < _dataLength; i += 4)
                                {
                                    if (i + 3 >= holdingRegs.Length)
                                        break; // Prevent out-of-range access

                                    // Combine four 16-bit registers into a 64-bit integer (use ulong to avoid truncation)
                                    ulong combined = ((ulong)holdingRegs[i] << 48)
                                                    | ((ulong)holdingRegs[i + 1] << 32)
                                                    | ((ulong)holdingRegs[i + 2] << 16)
                                                    | (ulong)holdingRegs[i + 3];

                                    // Convert the combined integer to a double
                                    byte[] bytes = BitConverter.GetBytes(combined);

                                    if (_selectedEndian is "Little Endian")
                                        Array.Reverse(bytes); // Ensure correct endianness

                                    double doubleValue = BitConverter.ToDouble(bytes, 0);
                                    newData.Add(new StringWrapper(doubleValue.ToString()));
                                    for(int j = 0; j < 3; j++)
                                        newData.Add(new StringWrapper("")); // Empty strings to offset
                                }
                                break;
                            // decimal
                            default:
                                for (int i = 0; i < _dataLength; i++)
                                    newData.Add(new StringWrapper(holdingRegs[i].ToString()));
                                break;
                        }

                        // Update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                    }
                    catch (Exception e)
                    {
                        FailResp(e);
                    }
                }
            }
        }

        private void ReadInputRegs(TcpClient mtc, IModbusMaster mM)
        {
            using (mtc)
            {
                while (_connectEngage && _isConnected)
                {
                    Thread.Sleep(_scanRate);
                    try
                    {
                        // Tally data request
                        RequestInc();

                        // Verify TCP connection
                        if (!mtc.Connected)
                        {
                            _isConnected = false;
                            OnPropertyChanged(nameof(IsConnected));
                            throw new Exception("Lost connection during coil reading.");
                        }

                        // Request data over TCP
                        ushort[] inputRegs = mM.ReadInputRegisters(_deviceId, _startAddress, _dataLength);

                        if (inputRegs == null || inputRegs.Length != _dataLength)
                            throw new Exception("Received null or inadequate response for coils.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Begin transforming data into a UI friendly data collection
                        var newData = new ObservableCollection<StringWrapper>();

                        // Loop through the received data and convert each piece into a StringWrapper, which is what the UI binds to.

                        //NOTE: This is where you'll implement the numeric base and endian control!
                        // Possibly even the ASCII control as well.
                        // You'll need to convert the received data based on that UI selection.
                        // Each piece of data will then be converted into a string to be displayed in the UI

                        // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "64 Bit Double"}
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
                        // Update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<StringWrapper>(newData);
                            OnPropertyChanged(nameof(ModbusData));
                        });
                    }
                    catch (Exception e)
                    {
                        FailResp(e);
                    }
                }
            }
        }

        private void RequestInc()
        {
            _numRequests++;
            OnPropertyChanged(nameof(NumRequests));
        }

        private void SuccessResp()
        {
            _numResponses++;
            _numOKs++;
            _errMess = "";

            OnPropertyChanged(nameof(NumResponses));
            OnPropertyChanged(nameof(NumOKs));
            OnPropertyChanged(nameof(ErrMess));
        }

        private void FailResp(Exception e)
        {
            if (e is IOException or SocketException)
            {
                _errMess = "Connection Failure: Verify Server Activity, DeviceID, and TCP settings.";
            }
            else if (e is TimeoutException)
            {
                _errMess = "Connection Failure: MODBUS Timeout.";
            }
            else if (e is SlaveException)
            {
                _errMess = "Connection Failure: Data Type and/or Query Length not compatible with Server.";
            }
            else
            {
                _errMess = "Unknown Error: " + e.Message;
            }

            _numResponses++;
            _numErrors++;

            OnPropertyChanged(nameof(ErrMess));
            OnPropertyChanged(nameof(NumResponses));
            OnPropertyChanged(nameof(NumErrors));
        }

        private ushort GetMaxLengthForStartAddress()
        {
            ushort cap = (ushort)(65535 - _startAddress); // inclusive cap
            return Math.Min((ushort)120, cap);
        }
    }
}
