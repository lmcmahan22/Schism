using NModbus;
using NModbus.Extensions.Enron;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.ObjectModel;

namespace Schism.Models
{
    
    // Functionality yoinked and trimmed from NMODBUS Github sample project :D

    //TODO:
    // - The below methods allow for different times of data polling. Connect these methods to the UI and allow users to select which one they want to use.
    // - Implement error injection, if not already here

    public class MODBUSService
    {

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
        private string _selectedDataType;
        private string _selectedNumericBase;
        private string _selectedEndian;
        private string _selectedADisplayType;

        // dropdowns
        private ObservableCollection<string> _dataType = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private ObservableCollection<string> _numericBase = new ObservableCollection<string> { "Integer", "Hexadecimal", "Binary", "32 Bit Float", "32 Bit SW. Float", "64 Bit Float", "64 Bit SW. Float" };
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
            get => _deviceID;
            set => _deviceID = value;
        }

        public ushort Length
        {
            get => _length;
            set => _length = value;
        }

        public ushort StartAddress
        {
            get => _startAddress;
            set => _startAddress = value;
        }

        public bool AsciiEnable
        {
            get => _asciiEnable;
            set => _asciiEnable = value;
        }

        public ObservableCollection<string> DataType => _dataType;
        public ObservableCollection<string> NumericBase => _numericBase;
        public ObservableCollection<string> Endian => _endian;
        public ObservableCollection<string> ADisplayType => _aDisplayType;

        public string SelectedDataType
        {
            get => _selectedDataType;
            set => _selectedDataType = value;
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

        // Results Collection
        public ObservableCollection<string> results = new ObservableCollection<string>();

        // Consutrctor
        private MODBUSService()
        {

            IpAddress = "165.165.165.11";
            TCPPort = 502;
            ScanRate = 1000;
            Timeout = 1000;
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

        public async Task<int> Connection(){

            try
            {
                await Task.Run(() => { });

                //ModbusTcpMasterReadHoldingRegisters32();

                switch (SelectedDataType)
                {

                    //{ "Coil Status", "Input Status", "Holding Registers", "Input Registers" }
                    case "Coil Status":
                        ReadCoils();
                        break;
                    case "Input Status":
                        ReadInputs();
                        break;
                    case "Holding Registers":
                        ReadHoldingRegs();
                        break;
                    case "Input Registers":
                        ReadInputRegs();
                        break;
                    default:
                        // This will never occur...
                        break;
                }
                
                //StartModbusTcpSlave();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();

            return 0;
        }

        private void ReadCoils()
        {
            // TODO: Implement once you get Read Inputs working!
        }

        private void ReadInputs()
        {
            IPAddress address = new IPAddress(Encoding.UTF8.GetBytes(IpAddress));

            // create and start the TCP slave
            TcpListener slaveTcpListener = new TcpListener(address, TCPPort);
            slaveTcpListener.Start();

            // Create the MODBUS factory, which handles MODBUS operations
            var factory = new ModbusFactory();
            var network = factory.CreateSlaveNetwork(slaveTcpListener);
            
            // Initialize MODBUS slave object
            IModbusSlave slave = factory.CreateSlave(DeviceID);
            network.AddSlave(slave);
            network.ListenAsync();

            // create the master
            TcpClient masterTcpClient = new TcpClient(address.ToString(), TCPPort);
            IModbusMaster master = factory.CreateMaster(masterTcpClient);

            // read five register values
            ushort[] inputs = master.ReadInputRegisters(0, StartAddress, Length);

            // TODO: Convert this into a list of data, dynamically sized.
            // This will be carried up into the UI.

            // Clear collection
            results.Clear();

            for (int i = 0; i < Length; i++)
            {
                results.Add(inputs[i].ToString());
            }

            // TODO: Instead of closing, poll repeatedly with respect to the polling period abvoe.
            // Does closing the app also close and stop these?
            masterTcpClient.Close();
            slaveTcpListener.Stop();

        }

        private void ReadHoldingRegs()
        {
            // TODO: Implement once you get Read Inputs working!
        }

        private void ReadInputRegs()
        {
            // TODO: Implement once you get Read Inputs working!
        }
    }
}
