// <copyright file="ModbusEngineCore.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models;
    using Schiism.Core.Models.Config;
    using Schiism.Core.Models.Enums;
    using Schiism.Core.Models.Snapshots;

    // Responsible for polling only
    // Engine → ModbusClient → Interpreter → Publisher
    public class ModbusEngineCore
    {
        // Ryan showed you a way to incorporate these not as instances, but as parameters from interfaces. You have the second part right...)
        private readonly IModbusClient modbusClient;
        private readonly ModbusInterpreter dataInterpreter;
        private readonly IDataPublisher dataPublisher;
        private readonly IEngineDiagnostics engineDiagnostics;

        // Private variables for engine internal state
        private int numOKs;
        private int numErrors;
        private int numRequests;
        private int numResponses;
        private string errMess;
        private bool isConnected;

        // Optional if you see connection drops at high poll rates...
        // private int _failureCount;
        // private const int FailureThreshold = 3;

        public ModbusEngineCore(
        IModbusClient client,
        ModbusInterpreter interpreter,
        IDataPublisher dataPublisher,
        IEngineDiagnostics engineDiag)
        {
            this.modbusClient = client;
            this.dataInterpreter = interpreter;
            this.dataPublisher = dataPublisher;
            this.engineDiagnostics = engineDiag;
            this.errMess = string.Empty;
        }

        public event Action<bool>? ConnectionChanged;

        public async Task RunAsync(ModbusConfig config, CancellationToken ct)
        {
            // While there is no desire to cancel (i.e. run in background constantly)
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Increment the number of requests sent (connection request)
                    this.RequestInc();

                    // Read raw data from the client class, with respect to the config class
                    List<ushort> rawData = this.modbusClient.ReadData(
                        config.IPAddress,
                        config.TCPPort,
                        config.DeviceId,
                        config.StartAddress,
                        config.DataLength,
                        config.TCPTimeout,
                        config.SelectedPollType);

                    // If the returned data is not what we expect, report an error
                    if (rawData == null || rawData.Count != config.DataLength)
                    {
                        throw new Exception("Received null or inadequate response for input registers.");
                    }

                    List<string> interp = new List<string>();

                    // Interpret only register data with the Interpreter class
                    if (config.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters)
                    {
                        interp = this.dataInterpreter.InterpretRegs(
                            rawData,
                            config.DataLength,
                            config.AsciiEnable,
                            config.SelectedDataSize,
                            config.SelectedNumericBase,
                            config.SelectedEndian);
                    }
                    else
                    {
                        // Nothing needs to change for digital data, just convert to strings.
                        interp = [.. rawData.Select(x => Convert.ToString(x))];
                    }

                    // Publish the parsed data to either a console or an IPC
                    // Use a constructor ???
                    DataSnapshotDto snap = new DataSnapshotDto
                    {
                        Data = interp,
                        DeviceId = config.DeviceId,
                        TimestampUtc = DateTime.UtcNow,
                    };

                    this.dataPublisher.PublishData(snap);
                    this.SetConnectionState(true); // This may seem strange, but in TCP, the only reliable signal is an already successful poll!
                    this.SuccessResp();

                    // Delay the polling loop, just as we used to
                    await Task.Delay(config.ScanRate, ct);
                }
                catch (Exception e)
                {
                    this.SetConnectionState(false);

                    // Increment the number of failed responses (data request)
                    this.FailResp(e);
                }
            }
        }

        private void RequestInc()
        {
            this.numRequests++;
        }

        private void SuccessResp()
        {
            this.numResponses++;
            this.numOKs++;

            // Clear error message, since we are now in a functional state
            this.errMess = string.Empty;
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
        }

        private void SetConnectionState(bool connected)
        {
            if (this.isConnected != connected)
            {
                this.isConnected = connected;
                this.ConnectionChanged?.Invoke(connected);
            }
        }
    }
}