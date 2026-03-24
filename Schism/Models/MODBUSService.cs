using NModbus;
using NModbus.Extensions.Enron;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
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

        // INotifyPropertyChanged interface for Models
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Singleton instance
        private static readonly Lazy<MODBUSService> _instance = new(() => new MODBUSService());
        public static MODBUSService Instance => _instance.Value;

        // Private variables
        private string _ipAddress;
        private int _tcpPort;
        private int _scanRate;
        private int _timeout;
        private int _pollDelay;
        private int _numPolls;
        private int _numOK;
        private int _numErrors;
        private int _numTX;
        private int _numRX;
        private int _numRequests;
        private int _numResponses;
        private byte _deviceID;
        private ushort _length;
        private ushort _startAddress; // Don't worry about leading zeros, Radzio just interprets a value without leading zeros as having leading zeroes (i.e. output coil range). Don't worry aboout "Global Data" either, since again Radzio doesn't bother.
        private bool _asciiEnable;
        private bool _isConnected;
        private string _selectedDataType;
        private string _selectedNumericBase;
        private string _selectedEndian;
        private string _selectedADisplayType;

        // dropdowns
        private ObservableCollection<string> _dataType = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private ObservableCollection<string> _numericBase = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" };
        private ObservableCollection<string> _endian = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Swap Words)", "Big Endian (Swap Bytes)" };
        private ObservableCollection<string> _aDisplayType = new ObservableCollection<string> { "1 Char/Reg", "2 Char/Reg", "2 Char/Reg SW." };

        // Properties for connection settings
        public string IpAddress
        {
            get => _ipAddress;
            set => _ipAddress = value;
        }

        public int TCPPort
        {
            get => _tcpPort;
            set => _tcpPort = value;
        }

        public int ScanRate
        {
            get => _scanRate;
            set => _scanRate = value;
        }

        public int Timeout
        {
            get => _timeout;
            set => _timeout = value;
        }

        public int PollDelay
        {
            get => _pollDelay;
            set => _pollDelay = value;
        }

        public int NumPolls
        {
            get => _numPolls;
            set => _numPolls = value;
        }

        public int NumOK
        {
            get => _numOK;
            set => _numOK = value;
        }

        public int NumErrors
        {
            get => _numErrors;
            set => _numErrors = value;
        }

        public int NumTX
        {
            get => _numTX;
            set => _numTX = value;
        }

        public int NumRX
        {
            get => _numRX;
            set => _numRX = value;
        }

        public int NumRequests
        {
            get => _numRequests;
            set => _numRequests = value;
        }

        public int NumResponses
        {
            get => _numResponses;
            set => _numResponses = value;
        }

        public byte DeviceID
        {
            get { return _deviceID; }
            set
            {
                // Min and Max boundaries on Device ID, according to MODBUS documentation
                byte clamped = Math.Clamp(value, (byte)1, (byte)247);
                if (_deviceID != clamped)
                {
                    _deviceID = clamped;
                    OnPropertyChanged();
                }
            }
        }

        public ushort Length
        {
            get { return _length; }
            set
            {
                // Min and Max boundaries on Value relative to current StartAddress
                ushort maxLen = GetMaxLengthForStartAddress();
                ushort clampedLength = Math.Clamp(value, (ushort)1, maxLen);

                if (_length != clampedLength)
                {
                    _length = clampedLength;

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
                    ushort clampedLength = Math.Clamp(_length, (ushort)1, maxLen);

                    if (_length != clampedLength)
                    {
                        _length = clampedLength;
                        // Notify the UI that the AddressList contents changed
                        OnPropertyChanged(nameof(Length));
                    }

                    // Notify StartAddress changed (caller/member name handled by OnPropertyChanged call above in SetProperty,
                    // but keeping parity with original behavior)
                    OnPropertyChanged();
                }
            }
        }

        public bool AsciiEnable
        {
            get { return _asciiEnable; }
            set
            {
                if (_asciiEnable != value)
                {
                    _asciiEnable = value;
                }
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> DataType => _dataType;
        public ObservableCollection<string> NumericBase => _numericBase;
        public ObservableCollection<string> Endian => _endian;
        public ObservableCollection<string> ADisplayType => _aDisplayType;

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
            set => _selectedNumericBase = value;
        }

        public string SelectedEndian
        {
            get => _selectedEndian;
            set => _selectedEndian = value;
        }

        public string SelectedADisplayType
        {
            get => _selectedADisplayType;
            set => _selectedADisplayType = value;
        }

        // ModbusData Collection
        private ObservableCollection<StringWrapper> _modbusData = new ObservableCollection<StringWrapper>();

        public ObservableCollection<StringWrapper> ModbusData
        {
            get => _modbusData;
            set
            {
                _modbusData = value;
                OnPropertyChanged();
            }
        }

        // Cleaner MVVM
        private ushort GetMaxLengthForStartAddress()
        {
            ushort cap = (ushort)(65535 - _startAddress); // inclusive cap
            return Math.Min((ushort)120, cap);
        }

        // Consutrctor
        private MODBUSService()
        {
            IpAddress = "165.165.165.11";
            TCPPort = 502;
            ScanRate = 1000;
            Timeout = 5000;
            PollDelay = 10;
            NumPolls = 0;
            NumOK = 0;
            NumErrors = 0;
            NumTX = 0;
            NumRX = 0;
            NumRequests = 0;
            NumResponses = 0;
            DeviceID = 1;
            Length = 10;
            StartAddress = 0;
            AsciiEnable = false;
            SelectedDataType = DataType.First();
            SelectedNumericBase = NumericBase.First();
            SelectedEndian = Endian.First();
            SelectedADisplayType = ADisplayType.First();
        }

        public async void Connection(){

            await Task.Run(() => MODBUSComms());
        }

        private void MODBUSComms()
        {
            try
            {
                IPAddress address = IPAddress.Parse(IpAddress);
                TcpClient masterTcpClient = new TcpClient(address.ToString(), TCPPort);
                // Create the MODBUS factory, which handles MODBUS operations
                var factory = new ModbusFactory();
                IModbusMaster modbusMaster = factory.CreateMaster(masterTcpClient);

                // Apply configurable timeout value from the connections window
                modbusMaster.Transport.ReadTimeout = Timeout;
                modbusMaster.Transport.WriteTimeout = Timeout;
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
                    bool[] coils = mM.ReadCoils(0, StartAddress, Length);
                    ushort[] coilsConv = coils.Select(Convert.ToUInt16).ToArray();

                    var newData = new List<List<StringWrapper>>();

                    for (int i = 0; i < 6; i++)
                        newData.Add(new List<StringWrapper>());

                    for (int i = 0; i < Length; i++)
                    {
                        newData[i / 20].Add(new StringWrapper(coilsConv[i].ToString()));
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < Results.Length; i++)
                        {
                            Results[i].Clear();

                            foreach (var item in newData[i])
                                Results[i].Add(item);
                        }
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
                    bool[] inputs = mM.ReadInputs(0, StartAddress, Length);
                    ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();

                    var newData = new List<List<StringWrapper>>();

                    for (int i = 0; i < 6; i++)
                        newData.Add(new List<StringWrapper>());

                    for (int i = 0; i < Length; i++)
                    {
                        newData[i / 20].Add(new StringWrapper(inputsConv[i].ToString()));
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < Results.Length; i++)
                        {
                            Results[i].Clear();

                            foreach (var item in newData[i])
                                Results[i].Add(item);
                        }
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
                    ushort[] holdingRegs = mM.ReadHoldingRegisters(0, StartAddress, Length);

                    var newData = new List<List<StringWrapper>>();

                    for (int i = 0; i < 6; i++)
                        newData.Add(new List<StringWrapper>());

                    for (int i = 0; i < Length; i++)
                    {
                        newData[i / 20].Add(new StringWrapper(holdingRegs[i].ToString()));
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < Results.Length; i++)
                        {
                            Results[i].Clear();

                            foreach (var item in newData[i])
                                Results[i].Add(item);
                        }
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
                    ushort[] inputRegs = mM.ReadInputRegisters(0, StartAddress, Length);

                    // empty 2D collection of data that will get populated
                    var newData = new List<List<StringWrapper>>();
                    for (int i = 0; i < 6; i++)
                        newData.Add(new List<StringWrapper>());

                    //NOTE: This is where you'll implement the numeric base and endian control!
                    // Possibly even the ASCII control as well.
                    // You'll need to convert the received data based on that UI selection.
                    // Each piece of data will then be converted into a string to be displayed in the UI

                    // { "Decimal", "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" }

                    switch (SelectedNumericBase)
                    {
                        case "Integer":
                            short[] holdingRegsSigned = new short[inputRegs.Length];
                            for (int i = 0; i < Length; i++)
                            {
                                holdingRegsSigned[i] = (short)inputRegs[i];
                                newData[i / 20].Add(new StringWrapper(holdingRegsSigned[i].ToString()));
                            }
                            break;
                        case "Hexadecimal":
                            for (int i = 0; i < Length; i++)
                            {
                                // the added "X" in the ToString parentheses does the conversion for us, since hex can't be parsed as a new numeric variable type
                                newData[i / 20].Add(new StringWrapper("0x"+inputRegs[i].ToString("X")));
                            }
                            break;
                        case "Binary":
                            string[] holdingRegsConv = new string[inputRegs.Length];
                            for (int i = 0; i < Length; i++)
                            {
                                string temp = Convert.ToString(inputRegs[i], 2); // 2 parameter converts value to a binary string
                                string paddedTemp = temp.PadLeft(16, '0');
                                string formattedTemp = Regex.Replace(paddedTemp, ".{4}", "$0 ").Trim();
                                newData[i / 20].Add(new StringWrapper(formattedTemp));
                            }
                            break;
                        // decimal
                        default:
                            for (int i = 0; i < Length; i++)
                            {
                                newData[i / 20].Add(new StringWrapper(inputRegs[i].ToString()));
                            }
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0; i < Results.Length; i++)
                        {
                            Results[i].Clear();

                            foreach (var item in newData[i])
                                Results[i].Add(item);
                        }
                    });

                    Thread.Sleep(ScanRate);
                }
                // Does closing the app also close and stop these?
                mtc.Close();
            }
        }
    }
}
