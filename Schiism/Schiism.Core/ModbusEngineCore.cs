// <copyright file="ModbusEngineCore.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Enums;
    using Schiism.Core.Models.Handlers;
    using Schiism.Core.Models.Snapshots;

    // Responsible for polling only
    // Engine → ModbusClient → Interpreter → Publisher
    public class ModbusEngineCore : IEngineService
    {
        // Ryan showed you a way to incorporate these not as instances, but as parameters from interfaces. You have the second part right...)
        private readonly IModbusClient modbusClient;
        private readonly IModbusInterpreter dataInterpreter;
        private readonly IDataPublisher dataPublisher;

        // Service logger used here via DI
        private readonly IEngineLogger _logger;

        private ModbusConfig? config;
        private CancellationTokenSource? internalCts;
        private Task? runningTask;

        // Private variables for engine internal state
        private string errMess = string.Empty;
        private bool isConnected;

        // Optional if you see connection drops at high poll rates...
        // private int _failureCount;
        // private const int FailureThreshold = 3;

        public event Action<bool>? ConnectionChanged;

        public ModbusEngineCore(
            IModbusClient client,
            IModbusInterpreter interpreter,
            IDataPublisher dataPublisher,
            IEngineLogger logger)
        {
            this.modbusClient = client;
            this.dataInterpreter = interpreter;
            this.dataPublisher = dataPublisher;
            this._logger = logger;
        }

        public void Configure(ModbusConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Task StartAsync(CancellationToken ct)
        {
            // Check for loaded configuration
            if (this.config == null)
            {
                throw new InvalidOperationException("Engine not configured.");
            }

            if (this.runningTask != null)
            {
                throw new InvalidOperationException("Engine already running.");
            }

            // define cancellation token
            this.internalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Define the task to be run
            this.runningTask = Task.Run(() => this.RunLoop(this.config, this.internalCts.Token));

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (this.internalCts == null)
            {
                return Task.CompletedTask;
            }

            this.internalCts.Cancel();

            return this.runningTask ?? Task.CompletedTask;
        }

        private async Task RunLoop(ModbusConfig config, CancellationToken ct)
        {

            this._logger.Info($"Attempting to poll device {config.DeviceId} at {config.IPAddress}...");

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

                    // This may seem strange, but in TCP, the only reliable signal is an already successful poll!
                    this.SetConnectionState(true);
                    this.SuccessResp();

                    // Delay the polling loop, just as we used to
                    await Task.Delay(config.ScanRate, ct);
                }
                catch (Exception e)
                {
                    // Similar to observing a successful poll, update to and document for a failed poll.
                    this.SetConnectionState(false);
                    this.FailResp(e, config.DeviceId);
                }
            }
        }

        private void RequestInc()
        {
            // this.numRequests++;
        }

        private void SuccessResp()
        {
            // this.numResponses++;
            // this.numOKs++;

            // Clear error message, since we are now in a functional state
            this.errMess = string.Empty;
        }

        private void FailResp(Exception e, Byte deviceID)
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

            // this.numResponses++;
            // this.numErrors++;

            this._logger.Error($"Modbus polling failed for device: {deviceID} @ {DateTime.UtcNow}.\nDetails: {this.errMess}", e);
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