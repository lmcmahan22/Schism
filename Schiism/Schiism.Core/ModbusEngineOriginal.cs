// <copyright file="ModbusEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using NModbus;
using Schiism.Core.Abstractions;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Schiism.Core
{
    public class ModbusEngineOriginal
    {
        // Private variables
        private int numOKs;
        private int numErrors;
        private int numRequests;
        private int numResponses;
        private bool connectEngage;
        private bool isConnected;

        // Raw MODBUS data collection
        private List<string> rawModbusData = new List<string>();

        // Consutrctor
        private ModbusEngineOriginal()
        {
            this.numRequests = 0;
            this.numResponses = 0;
            this.numOKs = 0;
            this.numErrors = 0;
            this.connectEngage = false;
            this.isConnected = false;
        }

        // Singleton instance
        public static ModbusEngineOriginal Instance { get; } = new();

        // Properties for connection settings
        public string IPAddr
        {
            get => ipAddr;
            set
            {
                if (ipAddr != value)
                {
                    ipAddr = value;
                }
            }
        }

        public int TCPPort
        {
            get => tcpPort;
            set
            {
                if (tcpPort != value)
                {
                    tcpPort = value;
                }
            }
        }

        public int ScanRate
        {
            get => scanRate;
            set
            {
                if (scanRate != value)
                {
                    scanRate = value;
                }
            }
        }

        public int TCPTimeout
        {
            get => tcpTimeout;
            set
            {
                if (tcpTimeout != value)
                {
                    tcpTimeout = value;
                }
            }
        }

        public int NumOKs
        {
            get => numOKs;
            set
            {
                if (numOKs != value)
                {
                    numOKs = value;
                }
            }
        }

        public int NumErrors
        {
            get => numErrors;
            set
            {
                if (numErrors != value)
                {
                    numErrors = value;
                }
            }
        }

        public int NumRequests
        {
            get => numRequests;
            set
            {
                if (numRequests != value)
                {
                    numRequests = value;
                }
            }
        }

        public int NumResponses
        {
            get => numResponses;
            set
            {
                if (numResponses != value)
                {
                    numResponses = value;
                }
            }
        }

        public byte DeviceId
        {
            get => deviceId;
            set
            {
                // We already verified that the value is within our desired boundaries, so we simply need to check for a difference.
                if (deviceId != value)
                {
                    deviceId = value;
                }
            }
        }

        public byte DataLength
        {
            get => dataLength;
            set
            {
                // Take the currently set Data Size into account
                byte minLen = GetMinLengthForStartAddress();

                // Take the currently set StartingAddress into account
                byte maxLen = GetMaxLengthForStartAddress();

                // We already verified that the value is within our desired boundaries, so we simply need to check for a difference.
                if (dataLength != value)
                {
                    dataLength = value;
                }
            }
        }

        public string StartAddress
        {
            get => startAddress;
            set
            {
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

                // We can now confirm that the attempted decimal converted value is a short (1-65535), so we can type cast it!
                ushort decVal = Convert.ToUInt16(attemptDecVal);

                // Update approved value onto the startAddress string
                if (Convert.ToUInt16(startAddress) != decVal)
                {
                    startAddress = decVal.ToString();

                    // Adjust length to this newly accepted startAddress, in the event that our original length is no longer compatible
                    byte maxLen = GetMaxLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(dataLength, (byte)1, maxLen);

                    if (dataLength != clampedDataLength)
                    {
                        dataLength = clampedDataLength;
                    }
                }
            }
        }

        public bool AsciiEnable
        {
            get => asciiEnable;
            set
            {
                if (asciiEnable != value)
                {
                    asciiEnable = value;
                }
            }
        }

        public bool ConnectEngage
        {
            get => connectEngage;
            set
            {
                if (connectEngage != value)
                {
                    connectEngage = value;
                }
            }
        }

        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                }
            }
        }

        public string ErrMess
        {
            get => errMess;
            set
            {
                if (errMess != value)
                {
                    errMess = value;
                }
            }
        }

        public string SelectedDataType
        {
            get => selectedDataType;
            set
            {
                if (selectedDataType != value)
                {
                    selectedDataType = value;
                }
            }
        }

        public string SelectedDataSize
        {
            get => selectedDataSize;
            set
            {
                if (selectedDataSize != value)
                {
                    selectedDataSize = value;

                    // Adjust length to this newly accepted startAddress, in the event that our original length is no longer compatible
                    byte minLen = GetMinLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(dataLength, minLen, (byte)120);

                    if (dataLength != clampedDataLength)
                    {
                        dataLength = clampedDataLength;
                    }
                }
            }
        }

        public string SelectedNumericBase
        {
            get => selectedNumericBase;
            set
            {
                if (selectedNumericBase != value)
                {
                    selectedNumericBase = value;
                }
            }
        }

        public string SelectedEndian
        {
            get => selectedEndian;
            set
            {
                if (selectedEndian != value)
                {
                    selectedEndian = value;
                }
            }
        }

        // RawModbusData List
        public List<string> RawModbusData => rawModbusData;

        // Asynchronous method to run our MODBUS TCP connection off of the main UI thread
        public async void Connection()
        {
            connectEngage = true;

            await Task.Run(() => MODBUSComms());
        }

        // MODBUS TCP connection logic, which works according to entered user parameters
        private void MODBUSComms()
        {
            TcpClient masterTcpClient = new TcpClient();

            // Regex \b0+(\d+) finds leading zeros at word boundaries and keeps the remaining digits
            string cleanedIp = Regex.Replace(ipAddr, @"\b0+(\d+)", "$1");
            IPAddress address = IPAddress.Parse(cleanedIp);
            ModbusFactory factory = new ModbusFactory();
            IModbusMaster modbusClientMaster;

            // Only attempt a connection while the user has prompted to do so (toggle the connection button)
            while (connectEngage)
            {
                try
                {
                    // Increment the number of requests sent (connection request)
                    RequestInc();

                    // Connection Request
                    masterTcpClient = new TcpClient(address.ToString(), tcpPort);
                    masterTcpClient.ReceiveTimeout = tcpTimeout;
                    masterTcpClient.SendTimeout = tcpTimeout;

                    // MODBUS connection details
                    modbusClientMaster = new ModbusFactory().CreateMaster(masterTcpClient);
                    modbusClientMaster.Transport.ReadTimeout = tcpTimeout;
                    modbusClientMaster.Transport.WriteTimeout = tcpTimeout;
                    modbusClientMaster.Transport.Retries = 0; // The connection attempt will retry by nature of this while loop, so we don't need retries here as well

                    isConnected = true;

                    // Works like a Try/Finally, but with the added benefit that the "Finally" contains a close function for the TCPClient object
                    using (masterTcpClient)
                    {
                        // Loop only while we're attempting to connect and actively connected
                        while (connectEngage && isConnected)
                        {
                            // Polling rate
                            Thread.Sleep(scanRate);

                            try
                            {
                                // Confirm that we haven't lost the connection since the last data poll. If we have, break out of this loop with an error
                                if (!masterTcpClient.Connected)
                                {
                                    isConnected = false;
                                    throw new Exception($"Lost connection during data reading.");
                                }

                                // Increment the number of requests sent (data request)
                                RequestInc();

                                // Prepare List that will replace the existing data collection, once populated
                                var newData = new List<string>();

                                // Hop into one of several individual polling methods, according to selectedDataType
                                switch (selectedDataType)
                                {
                                    case "Input Status":
                                        newData = ReadDigital(modbusMaster, true);
                                        break;
                                    case "Holding Registers":
                                        newData = ReadHoldingRegs(modbusMaster);
                                        break;
                                    case "Input Registers":
                                        newData = ReadInputRegs(modbusMaster);
                                        break;
                                    default:
                                        // Coils
                                        newData = ReadDigital(modbusMaster, false);
                                        break;
                                }

                                rawModbusData.Clear();
                                foreach (var item in newData)
                                {
                                    rawModbusData.Add(item);
                                }
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

        private void RequestInc()
        {
            numRequests++;
        }

        private void SuccessResp()
        {
            numResponses++;
            numOKs++;

            // Clear error message, since we are now in a functional state
            errMess = string.Empty;
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

                errMess = "Command Failure: Server did not accept the received MODBUS Command. Error Code: " + se.SlaveExceptionCode + " - \"" + details + "\"";
            }
            else if (e is IOException or SocketException)
            {
                errMess = "Connection Failure: Please verify Server activity, DeviceID, and TCP settings.";
            }
            else if (e is TimeoutException)
            {
                errMess = "Timeout Failure: Please assess connection integrity.";
            }
            else if (e is InvalidModbusRequestException)
            {
                errMess = "Command Failure: Sent MODBUS Command is not structurally sound.";
            }
            else if (e is NotImplementedException)
            {
                errMess = "Command Failure: Function Code is incompatible with Transport Type and/or NModbus Library.";
            }
            else
            {
                errMess = "Unknown Error: " + e.Message;
            }

            numResponses++;
            numErrors++;
        }

        // Prevent user from prompting a data overflow simply due to configuring the length and data size poorly
        private byte GetMinLengthForStartAddress()
        {
            return selectedDataSize switch
            {
                "32-Bit" => 2,
                "64-Bit" => 4,
                _ => 1, // "16-Bit" or default
            };
        }

        // Prevent user from prompting a data overflow simply due to configuring the length and starting address poorly
        private byte GetMaxLengthForStartAddress()
        {
            ushort startAdd = Convert.ToUInt16(startAddress);
            int cap = ushort.MaxValue - startAdd + 1; // inclusive cap (stroed as an int, because this could be 65536 in the event that the StartingAddress is curently 0. If so, that is okay, because 120 will end up being the minimum.
            ushort clamped = (ushort)Math.Min(120, cap);
            return (byte)clamped;
        }

        public ushort[] ReadHoldingRegisters(byte deviceId, ushort start, ushort length)
        {
            throw new NotImplementedException();
        }

        public ushort[] ReadInputRegisters(byte deviceId, ushort start, ushort length)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadCoils(byte deviceId, ushort start, ushort length)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadInputs(byte deviceId, ushort start, ushort length)
        {
            throw new NotImplementedException();
        }
    }
}
