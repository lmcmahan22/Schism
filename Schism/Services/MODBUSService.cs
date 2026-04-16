using NModbus;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Schism.Services
{
    public class MODBUSService : INotifyPropertyChanged, INotifyDataErrorInfo
    {

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // INotifyDataErrorInfo for startAddress string
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        // Error control methods (Get, Add, and Clear) to support the INotifyDataErrorInfo interface
        // Essentially, Errors are kept in a collection for easier tracking, if needed
        public IEnumerable? GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            return _errors.TryGetValue(propertyName, out var errors) ? errors : null;
        }

        protected void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }

        // Singleton instance
        public static MODBUSService Instance { get; } = new();

        // Private variables
        private string _ipAddr;
        private int _tcpPort;
        private int _scanRate;
        private int _tcpTimeout;
        private int _numOKs;
        private int _numErrors;
        private int _numRequests;
        private int _numResponses;
        private byte _deviceId;
        private ushort _dataLength;
        private bool _asciiEnable;
        private bool _connectEngage;
        private bool _isConnected;
        private string _errMess;

        // private _startAddress variable with custom string validation control
        private string _startAddress;
        private readonly Dictionary<string, List<string>> _errors = new();
        public bool HasErrors => _errors.Count > 0;
        private static readonly Regex StartAddressRegex = new(@"^(?:\d+[0-9]+|[0-9A-Fa-f]+h)$", RegexOptions.Compiled);

        // Dropdown selected variables
        private string _selectedDataType;
        private string _selectedDataSize;
        private string _selectedNumericBase;
        private string _selectedEndian;

        // dropdown contents (never change)
        private readonly ObservableCollection<string> _dataTypes = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private readonly ObservableCollection<string> _endians = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Byte-Swap)", "Little Endian (Byte-Swap)" };

        // dropdown contents (can be changed)
        private ObservableCollection<string> _dataSizes = new ObservableCollection<string> { "16-Bit", "32-Bit", "64-Bit" };
        private ObservableCollection<string> _numericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary" }; // "Floating Point" removed for now, but gets added to the list once the user selects 32-Bit or 64-Bit Data Size!

        // Raw MODBUS data collection
        private ObservableCollection<string> _rawModbusData = new ObservableCollection<string>();

        // Properties for connection settings
        public string IPAddr
        {
            get => _ipAddr;
            set
            {
                if (_ipAddr != value)
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

        public string StartAddress
        {
            get { return _startAddress; }
            set
            {

                // Validate string with respect to custom rules. If we fail this check, don't set the StartAddress
                ValidateStartAddress(value);

                if (_errors.Count > 0)
                {
                    return; // Don't execute the remaining set logic, since we've identified an invalid string
                    // since we don't update _startAddress, this simply reverts back to what the value was previously.
                    // We need to do it this way, because otherwise the ViewModel, or even the code below could reference an invalid string.
                }

                // temp variable to help store the eventual new value to be updated into _startAddress
                ushort decVal = 0;

                // If the value contains "h"
                if (value.Contains('h'))
                {
                    // Get rid of the "h" at the end ex. "Ah -> A"
                    string trun = value.Substring(0, value.Length - 1);

                    // convert hex string into a decimal int ex. "A -> 10"
                    decVal = Convert.ToUInt16(trun, 16);
                }
                // If the value contains just numbers (no "h")
                else
                    decVal = Convert.ToUInt16(value);

                // Min and Max boundaries on Starting Address, according to MODBUS documentation
                ushort clampedStart = Math.Clamp(decVal, (ushort)0, (ushort)65535);

                // If the numeric value represented by the current string can be updated, then do so.
                if (Convert.ToUInt16(_startAddress) != clampedStart)
                {
                    _startAddress = clampedStart.ToString();
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

        // Make Observable Collections public. None of these need Getters/Setters, by nature of ObservableCollections
        public ObservableCollection<string> DataTypes => _dataTypes;
        public ObservableCollection<string> Endians => _endians;

        // Modifiable ObservableCollections for dropdowns that can be changed by the user. This allows for dynamic updating of dropdown contents if needed in the future, while still exposing them to the UI for binding.
        public ObservableCollection<string> DataSizes
        {
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

        // RawModbusData ObservableCollection
        public ObservableCollection<string> RawModbusData => _rawModbusData;

        // Consutrctor
        private MODBUSService()
        {
            _ipAddr = "192.168.100.020";
            _tcpPort = 502;
            _scanRate = 500;
            _tcpTimeout = 5000;
            _numRequests = 0;
            _numResponses = 0;
            _numOKs = 0;
            _numErrors = 0;
            _deviceId = 1;
            _dataLength = 10;
            _startAddress = "0";
            _asciiEnable = false;
            _errMess = "";
            _selectedDataType = DataTypes.First();
            _selectedDataSize = DataSizes.First();
            _selectedNumericBase = NumericBases.First();
            _selectedEndian = Endians.First();
        }

        // Asynchronous method to run our MODBUS TCP connection off of the main UI thread
        public async void Connection() {

            _connectEngage = true;
            OnPropertyChanged(nameof(ConnectEngage));

            await Task.Run(() => MODBUSComms());
        }

        // MODBUS TCP connection logic, which works according to entered user parameters
        private void MODBUSComms()
        {
            TcpClient masterTcpClient = new TcpClient();
            // Regex \b0+(\d+) finds leading zeros at word boundaries and keeps the remaining digits
            string cleanedIp = Regex.Replace(_ipAddr, @"\b0+(\d+)", "$1");
            IPAddress address = IPAddress.Parse(cleanedIp);
            ModbusFactory factory = new ModbusFactory();
            IModbusMaster modbusMaster;

            // Only attempt a connection while the user has prompted to do so (toggle the connection button)
            while (_connectEngage)
            {
                try
                {
                    // Increment the number of requests sent (connection request)
                    RequestInc();

                    // Connection Request
                    masterTcpClient = new TcpClient(address.ToString(), _tcpPort);
                    masterTcpClient.ReceiveTimeout = _tcpTimeout;
                    masterTcpClient.SendTimeout = _tcpTimeout;

                    // MODBUS connection details
                    modbusMaster = new ModbusFactory().CreateMaster(masterTcpClient);
                    modbusMaster.Transport.ReadTimeout = _tcpTimeout;
                    modbusMaster.Transport.WriteTimeout = _tcpTimeout;
                    modbusMaster.Transport.Retries = 0; // The connection attempt will retry by nature of this while loop, so we don't need retries here as well

                    // Call back to the main UI thread to update successful connection status
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _isConnected = true;
                        OnPropertyChanged(nameof(IsConnected));
                    });

                    // Works like a Try/Finally, but with the added benefit that the "Finally" contains a close function for the TCPClient object
                    using (masterTcpClient)
                    {
                        // Loop only while we're attempting to connect and actively connected
                        while (_connectEngage && _isConnected)
                        {
                            // Polling rate
                            Thread.Sleep(_scanRate);

                            try
                            {
                                // Confirm that we haven't lost the connection since the last data poll. If we have, break out of this loop with an error
                                if (!masterTcpClient.Connected)
                                {
                                    _isConnected = false;
                                    OnPropertyChanged(nameof(IsConnected));
                                    throw new Exception($"Lost connection during data reading.");
                                }

                                // Increment the number of requests sent (data request)
                                RequestInc();

                                // Prepare ObservableCollection that will replace the existing data collection, once populated
                                var newData = new ObservableCollection<string>();

                                // Hop into one of several individual polling methods, according to selectedDataType
                                switch (_selectedDataType)
                                {
                                    case "Input Status":
                                        newData = ReadInputs(modbusMaster, newData);
                                        break;
                                    case "Holding Registers":
                                        newData = ReadHoldingRegs(modbusMaster, newData);
                                        break;
                                    case "Input Registers":
                                        newData = ReadInputRegs(modbusMaster, newData);
                                        break;
                                    default:
                                        // Coils
                                        newData = ReadCoils(modbusMaster, newData);
                                        break;
                                }

                                // Update the RawModbusData collection with the new data on the main UI thread
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _rawModbusData.Clear();
                                    foreach (var item in newData)
                                        _rawModbusData.Add(item);
                                    OnPropertyChanged(nameof(RawModbusData));
                                });
                            }
                            catch (Exception e)
                            {
                                // Increment the number of failed responses (data request)
                                FailResp(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Increment the number of fail responses (connection request)
                    FailResp(e);
                }
            }
        }

        // Read Coils attempt
        private ObservableCollection<string> ReadCoils(IModbusMaster mM, ObservableCollection<string> nD)
        {

            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(_startAddress);
            bool[] coils = mM.ReadCoils(_deviceId, startAdd, _dataLength);

            // If the returned data is not what we expect, report an error
            if (coils == null || coils.Length != _dataLength)
                throw new Exception("Received null or inadequate response for coils.");
            else
                // Report a successful TCP response, now that we have the data
                SuccessResp();

            // Begin transforming data into an ObservableCollection
            ushort[] coilsConv = coils.Select(Convert.ToUInt16).ToArray();

            // Loop through the received data and convert each piece into a string, for easier UI implementation
            for (int i = 0; i < coilsConv.Length; i++)
                nD.Add(coilsConv[i].ToString());

            // Return this collection so it can be forwarded up to the ViewModel
            return nD;
        }

        // Read Inputs attempt
        private ObservableCollection<string> ReadInputs(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(_startAddress);
            bool[] inputs = mM.ReadInputs(_deviceId, startAdd, _dataLength);

            // If the returned data is not what we expect, report an error
            if (inputs == null || inputs.Length != _dataLength)
                throw new Exception("Received null or inadequate response for inputs.");
            else
                // Report a successful TCP response, now that we have the data
                SuccessResp();

            // Begin transforming data into a UI friendly data collection
            ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();

            // Loop through the received data and convert each piece into a string, for easier UI implementation
            for (int i = 0; i < _dataLength; i++)
                nD.Add(inputsConv[i].ToString());

            // Return this collection so it can be forwarded up to the ViewModel
            return nD;
        }

        // Read Holding Registers attempt
        private ObservableCollection<string> ReadHoldingRegs(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(_startAddress);
            ushort[] holdingRegs = mM.ReadHoldingRegisters(_deviceId, startAdd, _dataLength);

            // If the returned data is not what we expect, report an error
            if (holdingRegs == null || holdingRegs.Length != _dataLength)
                throw new Exception("Received null or inadequate response for holding registers.");
            else
                // Report a successful TCP response, now that we have the data
                SuccessResp();

            // Convert registers to a parsed Observablecollection of strings using several helper methods
            // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
            return InterpetModbusData(holdingRegs);
        }

        // Read Input Registers attempt
        private ObservableCollection<string> ReadInputRegs(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(_startAddress);
            ushort[] inputRegs = mM.ReadInputRegisters(_deviceId, startAdd, _dataLength);

            // If the returned data is not what we expect, report an error
            if (inputRegs == null || inputRegs.Length != _dataLength)
                throw new Exception("Received null or inadequate response for input registers.");
            else
                // Report a successful TCP response, now that we have the data
                SuccessResp();

            // Convert registers to a parsed collection of strings using helper and update UI
            // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
            return InterpetModbusData(inputRegs);
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

            switch (_selectedEndian)
            {
                case "Little Endian":
                    // Reverse full array: [a,b,c,d] -> [d,c,b,a]
                    bytes.Reverse();
                    break;
                case "Big Endian (Byte-Swap)":
                    // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
                    SwapBytesWithinWords(bytes);
                    break;
                case "Little Endian (Byte-Swap)":
                    // Reverse full array then swap within each word: [a,b,c,d] -> [d,c,b,a] -> [c,d,a,b]
                    bytes.Reverse();
                    SwapBytesWithinWords(bytes);
                    break;
                default:
                    // "Big Endian" -> keep as-is
                    break;
            }
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

                        // Not a valid floating point width; fall back (not even possible to hit this with current UI handling)
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
                value = value << 8 | b;
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

            // Clear error message, since we are now in a functional state
            _errMess = "";

            OnPropertyChanged(nameof(NumResponses));
            OnPropertyChanged(nameof(NumOKs));
            OnPropertyChanged(nameof(ErrMess));
        }

        private void FailResp(Exception e)
        {
            // Error message for improved user clarity
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

        // Prevent user from prompting a data overflow simply due to configuring the length and starting address poorly
        private ushort GetMaxLengthForStartAddress()
        {
            ushort startAdd = Convert.ToUInt16(_startAddress);
            ushort cap = (ushort)(65535 - startAdd); // inclusive cap
            return Math.Min((ushort)120, cap);
        }

        private void ValidateStartAddress(string value) {

            ClearErrors(nameof(StartAddress));

            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(nameof(StartAddress), "Required");
            }
            else if (!StartAddressRegex.IsMatch(value))
            {
                AddError(nameof(StartAddress), "Must be decimal or hex (e.g. 1AFh)");
            }
        }
    }
}
