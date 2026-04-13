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
        private string _selectedDataSize;
        private string _selectedNumericBase;
        private string _selectedEndian;
        private string _selectedAsciiDisplayType;
        private string _errMess;

        // dropdown contents (never change)
        private readonly ObservableCollection<string> _dataTypes = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private readonly ObservableCollection<string> _endians = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Byte-Swap)", "Little Endian (Byte-Swap)" };
        private readonly ObservableCollection<string> _asciiDisplayTypes = new ObservableCollection<string> { "1 Char/Reg", "2 Char/Reg", "2 Char/Reg (Byte-Swap)" };

        // dropdown contents (can be changed)
        private ObservableCollection<string> _dataSizes = new ObservableCollection<string> { "16-Bit", "32-Bit", "64-Bit" };
        private  ObservableCollection<string> _numericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary"}; // "Floating Point" removed for now, but gets added to the list once the user selects 32-Bit or 64-Bit Data Size!

        // ModbusData Collection
        private ObservableCollection<string> _modbusData = new ObservableCollection<string>();

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

        public string SelectedDataSize
        {
            get => _selectedDataSize;
            set
            {
                if (_selectedDataSize != value)
                {
                    _selectedDataSize = value;
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
        public ObservableCollection<string> Endians => _endians;
        public ObservableCollection<string> AsciiDisplayTypes => _asciiDisplayTypes;

        // Modifiable ObservableCollections for dropdowns that can be changed by the user. This allows for dynamic updating of dropdown contents if needed in the future, while still exposing them to the UI for binding.
        public ObservableCollection<string> DataSizes{
            get => _dataSizes;
            set
            {
                if (_dataSizes != value)
                {
                    _dataSizes = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<string> NumericBases
        {
            get => _numericBases;
            set
            {
                if (_numericBases != value)
                {
                    _numericBases = value;
                    OnPropertyChanged();
                }
            }
        }

        // ModbusData ObservableCollection, which will have its data manipulated by this class.
        // This is the collection that the UI will bind to in order to display the received data. Whenever this collection is updated, the UI will automatically reflect those changes.
        public ObservableCollection<string> ModbusData => _modbusData;

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
            _selectedDataSize = DataSizes.First();
            _selectedNumericBase = NumericBases.First();
            _selectedEndian = Endians.First();
            _selectedAsciiDisplayType = AsciiDisplayTypes.First();
            _errMess= "";
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
                        var newData = new ObservableCollection<string>();

                        // Loop through the received data and convert each piece into a string, which is what the UI binds to.
                        for (int i = 0; i < coilsConv.Length; i++)
                            newData.Add(new string(coilsConv[i].ToString()));

                        // Update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<string>(newData);
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
                            throw new Exception("Received null or inadequate response for inputs.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Begin transforming data into a UI friendly data collection
                        ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();
                        var newData = new ObservableCollection<string>();

                        // Loop through the received data and convert each piece into a string, which is what the UI binds to.
                        for (int i = 0; i < _dataLength; i++)
                            newData.Add(new string(inputsConv[i].ToString()));

                        // Update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<string>(newData);
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
                            throw new Exception("Received null or inadequate response for holding registers.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Convert registers to a parsed collection of strings using helper and update UI
                        // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
                        ObservableCollection<string> newData = InterpetModbusData(holdingRegs);

                        // On the UI thread, update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<string>(newData);
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
                            throw new Exception("Received null or inadequate response for input registers.");
                        else
                            // Report a successful TCP response, now that we have the data
                            SuccessResp();

                        // Convert registers to a parsed collection of strings using helper and update UI
                        // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
                        ObservableCollection<string> newData = InterpetModbusData(inputRegs);

                        // On the UI thread, update the ModbusData collection with the new data, which will automatically update the UI due to data binding.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _modbusData = new ObservableCollection<string>(newData);
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

        // Helper Methods
        private ObservableCollection<string> InterpetModbusData(ushort[] receivedRegisters)
        {
            // Convert raw ushort registers into ObservableCollection<string> for UI display, applying user-selected transformations for data size, numeric base, endianness, and ASCII interpretation.
            var result = new ObservableCollection<string>();

            // Determine how many 16-bit registers compose one displayed value
            int regsPerValue = _selectedDataSize switch
            {
                "32-Bit" => 2,
                "64-Bit" => 4,
                _ => 1 // "16-Bit" or default
            };

            // Calculate total bit width for formatting purposes (16, 32, 64)
            int bitWidth = regsPerValue * 16;

            // Loop through the registers in chunks corresponding to the selected data size (1 register for 16-bit, 2 for 32-bit, 4 for 64-bit)
            for (int i = 0; i < _dataLength; i += regsPerValue)
            {
                if (i + regsPerValue - 1 >= receivedRegisters.Length)
                    break; // not enough registers remaining, scaled based on registers per value

                // Break the current chunk of registers into bytes in MSB-first order (per register).
                // For example if we're using 32-bit values (2 registers per value) [reg0, reg1], we get [reg0_hi, reg0_lo, reg1_hi, reg1_lo] for 4 total bytes.
                // The same occurs for 16-bit and 64-bit, just with different byte counts, 2 and 8 respectively.
                List<byte> bytes = new List<byte>(regsPerValue * 2);
                for (int j = 0; j < regsPerValue; j++)
                {
                    // i + j = the current value + the current register within that value
                    ushort reg = receivedRegisters[i + j];

                    // Add high byte then low byte for each register to get original ordering from each register (MSB/Big Endian)
                    bytes.Add((byte)(reg >> 8));
                    bytes.Add((byte)(reg & 0xFF));
                }

                // Apply endian transformation to the series of bytes acquired, based on the selected endian option.
                ApplyEndianTransformation(bytes);

                // Format value according to data size, numeric base, and ASCII enable selection (Hex only)
                string formatted = FormatBytes(bytes.ToArray(), bitWidth, _selectedNumericBase, _asciiEnable);

                // Add result to the collection as a string, which the UI binds to for display
                result.Add(new string(formatted));

                // For multi-register values, add placeholder cells to keep display alignment
                for (int pad = 1; pad < regsPerValue; pad++)
                    result.Add(new string(""));
            }

            return result;
        }

        private void ApplyEndianTransformation(List<byte> bytes)
        {
            // bytes currently MSB-first per register: [reg0_hi, reg0_lo, reg1_hi, reg1_lo, ...]
            // Handle selected endian options.
            if (_selectedEndian is "Little Endian")
            {
                bytes.Reverse();
                return;
            }

            if (_selectedEndian is "Big Endian (Byte-Swap)")
            {
                // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
                SwapBytesWithinWords(bytes);
                return;
            }

            if (_selectedEndian is "Little Endian (Byte-Swap)")
            {
                // Reverse full array then swap within each word:
                bytes.Reverse();
                SwapBytesWithinWords(bytes);
            }

            // "Big Endian" -> keep as-is
        }

        private static void SwapBytesWithinWords(List<byte> bytes)
        {
            // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
            for (int j = 0; j + 1 < bytes.Count; j += 2)
            {
                byte tmp = bytes[j];
                bytes[j] = bytes[j + 1];
                bytes[j + 1] = tmp;
            }
        }

        private string FormatBytes(byte[] bytes, int bitWidth, string numericBase, bool asciiEnable)
        {
            // Interpret bytes as MSB-first when manually constructing integers.
            // For floating point we need little-endian byte[] for BitConverter on typical platforms,
            // so reverse when calling BitConverter for floats/doubles.
            switch (numericBase)
            {
                case "Integer":
                    var le = bytes.Reverse().ToArray(); // BitConverter expects little-endian on typical platforms

                    // Use BitConverter to convert byte array to the appropriate integer type based on bit width, then convert to string for display.
                    return bitWidth switch
                    {
                        32 => BitConverter.ToInt32(le, 0).ToString(),
                        64 => BitConverter.ToInt64(le, 0).ToString(),
                        _ => BitConverter.ToInt16(le, 0).ToString() // "16-Bit" or default
                    };

                case "Hexadecimal":
                    {
                        // Convert byte array to an unsigned long for hex formatting, since hex is typically used for raw values regardless of signedness.
                        ulong unsigned = ToUnsigned(bytes);
                        string hex = bitWidth switch
                        {
                            32 => "0x" + unsigned.ToString("X8"),
                            64 => "0x" + unsigned.ToString("X16"),
                            _ => "0x" + unsigned.ToString("X4") // "16-Bit" or default
                        };

                        // Append ASCII contents, if enabled by user.
                        if (asciiEnable)
                        {
                            // Show ASCII interpreted from the current byte order
                            // NOTE: Contents will vary, depending on how many characters are placed in each register by the server
                            string ascii = Encoding.ASCII.GetString(bytes);
                            return "(" + ascii + ") " + hex;
                        }

                        return hex;
                    }

                case "Binary":
                    {
                        // Convert byte array to an unsigned long for binary formatting, since binary is typically used for raw values regardless of signedness.
                        ulong unsigned = ToUnsigned(bytes);

                        // Format binary with leading zeros based on bit width, and add spaces every 4 bits for readability.
                        string bin = Convert.ToString((long)unsigned, 2).PadLeft(bitWidth, '0');
                        string spaced = Regex.Replace(bin, ".{4}", "$0 ").Trim();
                        return spaced;
                    }

                case "Floating Point":
                    {
                        if (bitWidth == 32)
                        {
                            var lef = bytes.Reverse().ToArray(); // BitConverter expects little-endian on typical platforms
                            float f = BitConverter.ToSingle(lef, 0);
                            return f.ToString();
                        }

                        if (bitWidth == 64)
                        {
                            var led = bytes.Reverse().ToArray();
                            double d = BitConverter.ToDouble(led, 0);
                            return d.ToString();
                        }

                        // Not a valid floating point width; fall back
                        return "N/A";
                    }

                default: // Decimal (unsigned)
                    return ToUnsigned(bytes).ToString();
            }
        }

        private static ulong ToUnsigned(byte[] bytes)
        {
            // Build unsigned integer from byte array
            ulong value = 0;
            foreach (var b in bytes)
            {
                // Shift existing value left by 8 bits and add the next byte, effectively concatenating the bytes together in MSB-first order, now that we know we have the bytes in the order that we want for display.
                value = (value << 8) | b;
            }
            return value;
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
