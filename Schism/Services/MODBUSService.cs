// <copyright file="MODBUSService.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schism.Services
{
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
    using NModbus;

    public class MODBUSService : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private static readonly Regex StartAddressRegex = new(@"^(?:\d+|[0-9A-Fa-f]+h)$", RegexOptions.Compiled);
        private readonly Dictionary<string, List<string>> errors = new();

        // dropdown contents (never change)
        private readonly ObservableCollection<string> dataTypes = new ObservableCollection<string> { "Coil Status", "Input Status", "Holding Registers", "Input Registers" };
        private readonly ObservableCollection<string> endians = new ObservableCollection<string> { "Big Endian", "Little Endian", "Big Endian (Byte-Swap)", "Little Endian (Byte-Swap)" };

        // Private variables
        private string ipAddr;
        private int tcpPort;
        private int scanRate;
        private int tcpTimeout;
        private int numOKs;
        private int numErrors;
        private int numRequests;
        private int numResponses;
        private byte deviceId;
        private byte dataLength;
        private bool asciiEnable;
        private bool connectEngage;
        private bool isConnected;
        private string errMess;

        // private startAddress variable with custom string validation control
        private string startAddress;

        // Dropdown selected variables
        private string selectedDataType;
        private string selectedDataSize;
        private string selectedNumericBase;
        private string selectedEndian;

        // dropdown contents (can be changed)
        private ObservableCollection<string> dataSizes = new ObservableCollection<string> { "16-Bit", "32-Bit", "64-Bit" };
        private ObservableCollection<string> numericBases = new ObservableCollection<string> { "Decimal", "Integer", "Hexadecimal", "Binary" }; // "Floating Point" removed for now, but gets added to the list once the user selects 32-Bit or 64-Bit Data Size!

        // Raw MODBUS data collection
        private ObservableCollection<string> rawModbusData = new ObservableCollection<string>();

        // Consutrctor
        private MODBUSService()
        {
            this.ipAddr = "192.168.100.020";
            this.tcpPort = 502;
            this.scanRate = 500;
            this.tcpTimeout = 5000;
            this.numRequests = 0;
            this.numResponses = 0;
            this.numOKs = 0;
            this.numErrors = 0;
            this.deviceId = 1;
            this.dataLength = 10;
            this.startAddress = "0";
            this.asciiEnable = false;
            this.errMess = string.Empty;
            this.selectedDataType = this.DataTypes.First();
            this.selectedDataSize = this.DataSizes.First();
            this.selectedNumericBase = this.NumericBases.First();
            this.selectedEndian = this.Endians.First();
        }

        // INotifyPropertyChanged interface for Services
        public event PropertyChangedEventHandler? PropertyChanged;

        // INotifyDataErrorInfo for startAddress string
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        // Singleton instance
        public static MODBUSService Instance { get; } = new();

        public bool HasErrors => this.errors.Count > 0;

        // Properties for connection settings
        public string IPAddr
        {
            get => this.ipAddr;
            set
            {
                if (this.ipAddr != value)
                {
                    this.ipAddr = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int TCPPort
        {
            get => this.tcpPort;
            set
            {
                if (this.tcpPort != value)
                {
                    this.tcpPort = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int ScanRate
        {
            get => this.scanRate;
            set
            {
                if (this.scanRate != value)
                {
                    this.scanRate = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int TCPTimeout
        {
            get => this.tcpTimeout;
            set
            {
                if (this.tcpTimeout != value)
                {
                    this.tcpTimeout = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int NumOKs
        {
            get => this.numOKs;
            set
            {
                if (this.numOKs != value)
                {
                    this.numOKs = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int NumErrors
        {
            get => this.numErrors;
            set
            {
                if (this.numErrors != value)
                {
                    this.numErrors = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int NumRequests
        {
            get => this.numRequests;
            set
            {
                if (this.numRequests != value)
                {
                    this.numRequests = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public int NumResponses
        {
            get => this.numResponses;
            set
            {
                if (this.numResponses != value)
                {
                    this.numResponses = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public byte DeviceId
        {
            get => this.deviceId;
            set
            {
                // Validate incoming byte with respect to valid range (1-247)
                this.ValidateDevID(value);

                // Store the error status after the above validation call
                bool status = this.ErrorPresent(nameof(this.DeviceId));
                if (status)
                {
                    return; // Don't execute the remaining set logic, since we've identified an invalid incoming byte
                }

                // We already verified that the value is within our desired boundaries, so we simply need to check for a difference.
                if (this.deviceId != value)
                {
                    this.deviceId = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public byte DataLength
        {
            get => this.dataLength;
            set
            {
                // Take the currently set Data Size into account
                byte minLen = this.GetMinLengthForStartAddress();

                // Take the currently set StartingAddress into account
                byte maxLen = this.GetMaxLengthForStartAddress();

                // Validate incoming byte with respect to valid range (1-120)
                this.ValidateLength(value, minLen, maxLen);

                // Store the error status after the above validation call
                bool status = this.ErrorPresent(nameof(this.DataLength));
                if (status)
                {
                    return; // Don't execute the remaining set logic, since we've identified an invalid incoming byte
                }

                // We already verified that the value is within our desired boundaries, so we simply need to check for a difference.
                if (this.dataLength != value)
                {
                    this.dataLength = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public string StartAddress
        {
            get => this.startAddress;
            set
            {
                // Validate incoming string with respect to our addressing regex (see method)
                this.ValidateStartAddress(value);

                // Store the error status after the above validation call
                bool status = this.ErrorPresent(nameof(this.StartAddress));
                if (status)
                {
                    return; // Don't execute the remaining set logic, since we've identified an invalid incoming string
                }

                // temp variable to help store the incoming decimal value, after possible hex conversion
                uint attemptDecVal = 0;

                // If the value contains "h"
                if (value.Contains('h'))
                {
                    // Get rid of the "h" at the end ex. "Ah -> A"
                    string trun = value.Substring(0, value.Length - 1);

                    // convert hex string into a decimal int ex. "A -> 10"
                    attemptDecVal = Convert.ToUInt32(trun, 16);
                }

                // If the value contains just numbers (no "h")
                else
                {
                    attemptDecVal = Convert.ToUInt32(value);
                }

                // Validate converted decimal value with respect to valid range (0-65535)
                this.ValidateStartAddressConv(attemptDecVal);

                // Store the error status after the above validation call
                bool convStatus = this.ErrorPresent(nameof(this.StartAddress));
                if (convStatus)
                {
                    return; // Don't execute the remaining set logic, since we've identified an invalid incoming converted decimal value
                }

                // We can now confirm that the attempted decimal converted value is a short (1-65535), so we can type cast it!
                ushort decVal = Convert.ToUInt16(attemptDecVal);

                // Update approved value onto the startAddress string
                if (Convert.ToUInt16(this.startAddress) != decVal)
                {
                    this.startAddress = decVal.ToString();
                    this.OnPropertyChanged();

                    // Adjust length to this newly accepted startAddress, in the event that our original length is no longer compatible
                    byte maxLen = this.GetMaxLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(this.dataLength, (byte)1, maxLen);

                    if (this.dataLength != clampedDataLength)
                    {
                        this.dataLength = clampedDataLength;
                        this.OnPropertyChanged(nameof(this.DataLength)); // notify DataLength
                    }
                }
            }
        }

        public bool AsciiEnable
        {
            get => this.asciiEnable;
            set
            {
                if (this.asciiEnable != value)
                {
                    this.asciiEnable = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public bool ConnectEngage
        {
            get => this.connectEngage;
            set
            {
                if (this.connectEngage != value)
                {
                    this.connectEngage = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => this.isConnected;
            set
            {
                if (this.isConnected != value)
                {
                    this.isConnected = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public string ErrMess
        {
            get => this.errMess;
            set
            {
                if (this.errMess != value)
                {
                    this.errMess = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public string SelectedDataType
        {
            get => this.selectedDataType;
            set
            {
                if (this.selectedDataType != value)
                {
                    this.selectedDataType = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public string SelectedDataSize
        {
            get => this.selectedDataSize;
            set
            {
                if (this.selectedDataSize != value)
                {
                    this.selectedDataSize = value;
                    this.OnPropertyChanged();

                    // Adjust length to this newly accepted startAddress, in the event that our original length is no longer compatible
                    byte minLen = this.GetMinLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(this.dataLength, minLen, (byte)120);

                    if (this.dataLength != clampedDataLength)
                    {
                        this.dataLength = clampedDataLength;
                        this.OnPropertyChanged(nameof(this.DataLength)); // notify DataLength
                    }
                }
            }
        }

        public string SelectedNumericBase
        {
            get => this.selectedNumericBase;
            set
            {
                if (this.selectedNumericBase != value)
                {
                    this.selectedNumericBase = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public string SelectedEndian
        {
            get => this.selectedEndian;
            set
            {
                if (this.selectedEndian != value)
                {
                    this.selectedEndian = value;
                    this.OnPropertyChanged();
                }
            }
        }

        // Make Observable Collections public. None of these need Getters/Setters, by nature of ObservableCollections
        public ObservableCollection<string> DataTypes => this.dataTypes;

        public ObservableCollection<string> Endians => this.endians;

        // Modifiable ObservableCollections for dropdowns that can be changed by the user. This allows for dynamic updating of dropdown contents if needed in the future, while still exposing them to the UI for binding.
        public ObservableCollection<string> DataSizes
        {
            get => this.dataSizes;
            set
            {
                if (this.dataSizes != value)
                {
                    this.dataSizes = value;
                    this.OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> NumericBases
        {
            get => this.numericBases;
            set
            {
                if (this.numericBases != value)
                {
                    this.numericBases = value;
                    this.OnPropertyChanged();
                }
            }
        }

        // RawModbusData ObservableCollection
        public ObservableCollection<string> RawModbusData => this.rawModbusData;

        // Asynchronous method to run our MODBUS TCP connection off of the main UI thread
        public async void Connection()
        {
            this.connectEngage = true;
            this.OnPropertyChanged(nameof(this.ConnectEngage));

            await Task.Run(() => this.MODBUSComms());
        }

        // Error control methods (Get, Add, and Clear) to support the INotifyDataErrorInfo interface
        // Essentially, Errors are kept in a collection for easier tracking, if needed
        public IEnumerable? GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            return this.errors.TryGetValue(propertyName, out var errors) ? errors : null;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnErrorsChanged(string propertyName)
        {
            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected void AddError(string propertyName, string error)
        {
            if (!this.errors.ContainsKey(propertyName))
            {
                this.errors[propertyName] = new List<string>();
            }

            if (!this.errors[propertyName].Contains(error))
            {
                this.errors[propertyName].Add(error);
                this.OnErrorsChanged(propertyName);
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (this.errors.Remove(propertyName))
            {
                this.OnErrorsChanged(propertyName);
            }
        }

        private bool ErrorPresent(string propertyName)
        {
            return this.errors.TryGetValue(propertyName, out var list) && list.Count > 0;
        }

        // MODBUS TCP connection logic, which works according to entered user parameters
        private void MODBUSComms()
        {
            TcpClient masterTcpClient = new TcpClient();

            // Regex \b0+(\d+) finds leading zeros at word boundaries and keeps the remaining digits
            string cleanedIp = Regex.Replace(this.ipAddr, @"\b0+(\d+)", "$1");
            IPAddress address = IPAddress.Parse(cleanedIp);
            ModbusFactory factory = new ModbusFactory();
            IModbusMaster modbusMaster;

            // Only attempt a connection while the user has prompted to do so (toggle the connection button)
            while (this.connectEngage)
            {
                try
                {
                    // Increment the number of requests sent (connection request)
                    this.RequestInc();

                    // Connection Request
                    masterTcpClient = new TcpClient(address.ToString(), this.tcpPort);
                    masterTcpClient.ReceiveTimeout = this.tcpTimeout;
                    masterTcpClient.SendTimeout = this.tcpTimeout;

                    // MODBUS connection details
                    modbusMaster = new ModbusFactory().CreateMaster(masterTcpClient);
                    modbusMaster.Transport.ReadTimeout = this.tcpTimeout;
                    modbusMaster.Transport.WriteTimeout = this.tcpTimeout;
                    modbusMaster.Transport.Retries = 0; // The connection attempt will retry by nature of this while loop, so we don't need retries here as well

                    // Call back to the main UI thread to update successful connection status
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.isConnected = true;
                        this.OnPropertyChanged(nameof(this.IsConnected));
                    });

                    // Works like a Try/Finally, but with the added benefit that the "Finally" contains a close function for the TCPClient object
                    using (masterTcpClient)
                    {
                        // Loop only while we're attempting to connect and actively connected
                        while (this.connectEngage && this.isConnected)
                        {
                            // Polling rate
                            Thread.Sleep(this.scanRate);

                            try
                            {
                                // Confirm that we haven't lost the connection since the last data poll. If we have, break out of this loop with an error
                                if (!masterTcpClient.Connected)
                                {
                                    this.isConnected = false;
                                    this.OnPropertyChanged(nameof(this.IsConnected));
                                    throw new Exception($"Lost connection during data reading.");
                                }

                                // Increment the number of requests sent (data request)
                                this.RequestInc();

                                // Prepare ObservableCollection that will replace the existing data collection, once populated
                                var newData = new ObservableCollection<string>();

                                // Hop into one of several individual polling methods, according to selectedDataType
                                switch (this.selectedDataType)
                                {
                                    case "Input Status":
                                        newData = this.ReadInputs(modbusMaster, newData);
                                        break;
                                    case "Holding Registers":
                                        newData = this.ReadHoldingRegs(modbusMaster, newData);
                                        break;
                                    case "Input Registers":
                                        newData = this.ReadInputRegs(modbusMaster, newData);
                                        break;
                                    default:
                                        // Coils
                                        newData = this.ReadCoils(modbusMaster, newData);
                                        break;
                                }

                                // Update the RawModbusData collection with the new data on the main UI thread
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    this.rawModbusData.Clear();
                                    foreach (var item in newData)
                                    {
                                        this.rawModbusData.Add(item);
                                    }

                                    this.OnPropertyChanged(nameof(this.RawModbusData));
                                });
                            }
                            catch (Exception e)
                            {
                                // Increment the number of failed responses (data request)
                                this.FailResp(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Increment the number of fail responses (connection request)
                    this.FailResp(e);
                }
            }
        }

        // Read Coils attempt
        private ObservableCollection<string> ReadCoils(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(this.startAddress);
            bool[] coils = mM.ReadCoils(this.deviceId, startAdd, this.dataLength);

            // If the returned data is not what we expect, report an error
            if (coils == null || coils.Length != this.dataLength)
            {
                throw new Exception("Received null or inadequate response for coils.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                this.SuccessResp();
            }

            //// Begin transforming data into an ObservableCollection
            // ushort[] coilsConv = coils.Select(Convert.ToUInt16).ToArray();

            //// Loop through the received data and convert each piece into a string, for easier UI implementation
            // for (int i = 0; i < coilsConv.Length; i++)
            // {
            //    nD.Add(coilsConv[i].ToString());
            // }

            // New approach
            nD = [.. coils.Select(x => Convert.ToInt16(x).ToString())];

            // Return this collection so it can be forwarded up to the ViewModel
            return nD;
        }

        // Read Inputs attempt
        private ObservableCollection<string> ReadInputs(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(this.startAddress);
            bool[] inputs = mM.ReadInputs(this.deviceId, startAdd, this.dataLength);

            // If the returned data is not what we expect, report an error
            if (inputs == null || inputs.Length != this.dataLength)
            {
                throw new Exception("Received null or inadequate response for inputs.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                this.SuccessResp();
            }

            // Begin transforming data into a UI friendly data collection
            ushort[] inputsConv = inputs.Select(Convert.ToUInt16).ToArray();

            // Loop through the received data and convert each piece into a string, for easier UI implementation
            for (int i = 0; i < this.dataLength; i++)
            {
                nD.Add(inputsConv[i].ToString());
            }

            // Return this collection so it can be forwarded up to the ViewModel
            return nD;
        }

        // Read Holding Registers attempt
        private ObservableCollection<string> ReadHoldingRegs(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(this.startAddress);
            ushort[] holdingRegs = mM.ReadHoldingRegisters(this.deviceId, startAdd, this.dataLength);

            // If the returned data is not what we expect, report an error
            if (holdingRegs == null || holdingRegs.Length != this.dataLength)
            {
                throw new Exception("Received null or inadequate response for holding registers.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                this.SuccessResp();
            }

            // Convert registers to a parsed Observablecollection of strings using several helper methods
            // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
            return this.InterpetModbusData(holdingRegs);
        }

        // Read Input Registers attempt
        private ObservableCollection<string> ReadInputRegs(IModbusMaster mM, ObservableCollection<string> nD)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(this.startAddress);
            ushort[] inputRegs = mM.ReadInputRegisters(this.deviceId, startAdd, this.dataLength);

            // If the returned data is not what we expect, report an error
            if (inputRegs == null || inputRegs.Length != this.dataLength)
            {
                throw new Exception("Received null or inadequate response for input registers.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                this.SuccessResp();
            }

            // Convert registers to a parsed collection of strings using helper and update UI
            // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
            return this.InterpetModbusData(inputRegs);
        }

        // Helper Methods
        private ObservableCollection<string> InterpetModbusData(ushort[] receivedRegisters)
        {
            // Convert raw ushort registers into ObservableCollection<string> for UI display, applying user-selected transformations for data size, numeric base, endianness, and ASCII interpretation.
            var result = new ObservableCollection<string>();

            // Determine how many 16-bit registers compose one displayed value
            int regsPerValue = this.selectedDataSize switch
            {
                "32-Bit" => 2,
                "64-Bit" => 4,
                _ => 1, // "16-Bit" or default
            };

            // Calculate total bit width for formatting purposes (16, 32, 64)
            int bitWidth = regsPerValue * 16;

            // Loop through the registers in chunks corresponding to the selected data size (1 register for 16-bit, 2 for 32-bit, 4 for 64-bit)
            for (int i = 0; i < this.dataLength; i += regsPerValue)
            {
                if (i + regsPerValue - 1 >= receivedRegisters.Length)
                {
                    break; // not enough registers remaining, scaled based on registers per value
                }

                // Break the current chunk of registers into bytes in MSB-first order (per register).
                // For example if we're using 32-bit values (2 registers per value) [reg0, reg1], we get [reg0hi, reg0lo, reg1hi, reg1lo] for 4 total bytes.
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
                this.ApplyEndianTransformation(bytes);

                // Format value according to data size, numeric base, and ASCII enable selection (Hex only)
                string formatted = this.FormatBytes(bytes.ToArray(), bitWidth, this.selectedNumericBase, this.asciiEnable);

                // Add result to the collection as a string, which the UI binds to for display
                result.Add(new string(formatted));

                // For multi-register values, add placeholder cells to keep display alignment
                for (int pad = 1; pad < regsPerValue; pad++)
                {
                    result.Add(new string(string.Empty));
                }
            }

            return result;
        }

        private void ApplyEndianTransformation(List<byte> bytes)
        {
            // bytes currently MSB-first per register: [reg0hi, reg0lo, reg1hi, reg1lo, ...]
            // Handle selected endian options.
            switch (this.selectedEndian)
            {
                case "Little Endian":
                    // Reverse full array: [a,b,c,d] -> [d,c,b,a]
                    bytes.Reverse();
                    break;
                case "Big Endian (Byte-Swap)":
                    // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
                    this.SwapBytesWithinWords(bytes);
                    break;
                case "Little Endian (Byte-Swap)":
                    // Reverse full array then swap within each word: [a,b,c,d] -> [d,c,b,a] -> [c,d,a,b]
                    bytes.Reverse();
                    this.SwapBytesWithinWords(bytes);
                    break;
                default:
                    // "Big Endian" -> keep as-is
                    break;
            }
        }

        private void SwapBytesWithinWords(List<byte> bytes)
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
                        _ => BitConverter.ToInt16(le, 0).ToString(), // "16-Bit" or default
                    };

                case "Hexadecimal":
                    {
                        // Convert byte array to an unsigned long for hex formatting, since hex is typically used for raw values regardless of signedness.
                        ulong unsigned = this.ToUnsigned(bytes);
                        string hex = bitWidth switch
                        {
                            32 => "0x" + unsigned.ToString("X8"),
                            64 => "0x" + unsigned.ToString("X16"),
                            _ => "0x" + unsigned.ToString("X4"), // "16-Bit" or default
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
                        ulong unsigned = this.ToUnsigned(bytes);

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
                    return this.ToUnsigned(bytes).ToString();
            }
        }

        private ulong ToUnsigned(byte[] bytes)
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
            this.numRequests++;
            this.OnPropertyChanged(nameof(this.NumRequests));
        }

        private void SuccessResp()
        {
            this.numResponses++;
            this.numOKs++;

            // Clear error message, since we are now in a functional state
            this.errMess = string.Empty;

            this.OnPropertyChanged(nameof(this.NumResponses));
            this.OnPropertyChanged(nameof(this.NumOKs));
            this.OnPropertyChanged(nameof(this.ErrMess));
        }

        private void FailResp(Exception e)
        {
            // Error messages for user clarity
            if (e is SlaveException se)
            {
                string details = string.Empty;
                switch (se.SlaveExceptionCode)
                {
                    case SlaveExceptionCodes.IllegalFunction:
                        details = "Illegal Function";
                        break;

                    case SlaveExceptionCodes.IllegalDataAddress:
                        details = "Illegal Data Address";
                        break;

                    case SlaveExceptionCodes.IllegalDataValue:
                        details = "Illegal Data Value";
                        break;

                    case SlaveExceptionCodes.SlaveDeviceFailure:
                        details = "Server Device Failure";
                        break;
                    default:
                        details = "Undefined Error";
                        break;
                }

                this.errMess = "Command Failure: Server did not accept the received MODBUS Command. Error Code: " + se.SlaveExceptionCode + " - \"" + details + "\"";
            }
            else if (e is IOException or SocketException)
            {
                this.errMess = "Connection Failure: Please verify Server activity, DeviceID, and TCP settings.";
            }
            else if (e is TimeoutException)
            {
                this.errMess = "Timeout Failure: Please assess connection integrity.";
            }
            else if (e is InvalidModbusRequestException)
            {
                this.errMess = "Command Failure: Sent MODBUS Command is not structurally sound.";
            }
            else if (e is NotImplementedException)
            {
                this.errMess = "Command Failure: Function Code is incompatible with Transport Type and/or NModbus Library.";
            }
            else
            {
                this.errMess = "Unknown Error: " + e.Message;
            }

            this.numResponses++;
            this.numErrors++;

            this.OnPropertyChanged(nameof(this.ErrMess));
            this.OnPropertyChanged(nameof(this.NumResponses));
            this.OnPropertyChanged(nameof(this.NumErrors));
        }

        // Prevent user from prompting a data overflow simply due to configuring the length and data size poorly
        private byte GetMinLengthForStartAddress()
        {
            return this.selectedDataSize switch
            {
                "32-Bit" => 2,
                "64-Bit" => 4,
                _ => 1, // "16-Bit" or default
            };
        }

        // Prevent user from prompting a data overflow simply due to configuring the length and starting address poorly
        private byte GetMaxLengthForStartAddress()
        {
            ushort startAdd = Convert.ToUInt16(this.startAddress);
            int cap = (ushort.MaxValue - startAdd) + 1; // inclusive cap (stroed as an int, because this could be 65536 in the event that the StartingAddress is curently 0. If so, that is okay, because 120 will end up being the minimum.
            ushort clamped = (ushort)Math.Min(120, cap);
            return (byte)clamped;
        }

        private void ValidateDevID(byte value)
        {
            this.ClearErrors(nameof(this.DeviceId));

            if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                this.AddError(nameof(this.DeviceId), "Value Required");
            }

            // Value doesn't satisfy validating condition
            else if (value < 1 || value > 247)
            {
                this.AddError(nameof(this.DeviceId), "Must be between 1 and 247");
            }
        }

        private void ValidateStartAddress(string value)
        {
            this.ClearErrors(nameof(this.StartAddress));

            if (string.IsNullOrWhiteSpace(value))
            {
                this.AddError(nameof(this.StartAddress), "Value Required");
            }
            else if (!StartAddressRegex.IsMatch(value))
            {
                this.AddError(nameof(this.StartAddress), "Must be unsigned decimal or hex (e.g. \"1AFh\")");
            }
        }

        private void ValidateStartAddressConv(uint value)
        {
            this.ClearErrors(nameof(this.StartAddress));

            // Value doesn't satisfy validating condition
            if (value < 0 || value > 65535)
            {
                this.AddError(nameof(this.StartAddress), "Must be between 0 and 65535 (after hex conversion)");
            }
        }

        private void ValidateLength(byte value, byte min, byte max)
        {
            this.ClearErrors(nameof(this.DataLength));

            if (string.IsNullOrWhiteSpace(value.ToString()))
            {
                this.AddError(nameof(this.DataLength), "Value Required");
            }

            // Value doesn't satisfy validating condition
            else if (value < min || value > max)
            {
                this.AddError(nameof(this.DataLength), "Must be between 1 - 120 (or between fluid minimum and maximum, depending on Data Size and Starting Address parameters respectively)");
            }
        }
    }
}
