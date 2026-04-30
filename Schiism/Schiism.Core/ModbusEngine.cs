// <copyright file="ModbusEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Enums;
    using Schiism.Core.Models.Snapshots;
    using Schiism.Core.Models.Wrappers;

    /// <summary>
    /// Main MODBUS Engine class.
    /// Implements EngineService and uses the following via Dependency Injection:
    ///     - client
    ///     - ModbusInterpreter
    ///     - DataPublisher
    ///     - EnginePublisher
    ///     - NOTE: Including these at the top of the class removes the need for an explicit constructor! These are readonly!).
    /// Contains its own instance of CommsMetrics, which the Engine only has context to at the top level (here).
    /// </summary>
    public class ModbusEngine(IModbusConfig config, IModbusClient client, IModbusInterpreter interpreter, IDataPublisher dataPublisher, IEnginePublisher enginePublisher) : IModbusEngine
    {
        /// <inheritdoc/>
        public required CommsMetrics CommsMetr { get; set; }

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken token)
        {
            if (config == null)
            {
                throw new InvalidOperationException("Engine not configured.");
            }

            while (!token.IsCancellationRequested)
            {
                await this.RunLoop(config, token);
                await Task.Delay(config.ScanRate, token);
            }
        }

        private async Task RunLoop(IModbusConfig config, CancellationToken ct)
        {
            await Task.Yield();

            enginePublisher.Info($"Attempting to poll device {config.DeviceId} at {config.IPAddress}:");

            while (!ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    this.RequestInc();

                    List<ushort> rawData = client.ReadData(config);
                    if (rawData == null || rawData.Count != config.DataLength)
                    {
                        throw new Exception("Received null or inadequate response for input registers.");
                    }

                    List<string> interp = new List<string>();
                    if (config.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters)
                    {
                        interp = interpreter.InterpretRegs(config, rawData);
                    }
                    else
                    {
                        interp = [.. rawData.Select(x => Convert.ToString(x))];
                    }

                    DataSnapshotDto snap = new DataSnapshotDto
                    {
                        Data = interp,
                        DeviceId = config.DeviceId,
                    };
                    dataPublisher.PublishData(snap);

                    this.CommsMetr.IsConnected = true;
                    this.SuccessResp();

                    await Task.Delay(config.ScanRate, ct);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    this.CommsMetr.IsConnected = false;
                    this.FailResp(e, config.DeviceId);
                }
            }
        }

        private void RequestInc()
        {
            this.CommsMetr.NumRequests++;
        }

        private void SuccessResp()
        {
            this.CommsMetr.NumResponses++;
            this.CommsMetr.NumOKs++;
            this.CommsMetr.ErrorMessage = string.Empty;
        }

        private void FailResp(Exception e, byte deviceID)
        {
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

                this.CommsMetr.ErrorMessage = "Command Failure: Server did not accept the received MODBUS Command. Error Code: " + se.SlaveExceptionCode + " - \"" + details + "\"";
            }
            else if (e is IOException or SocketException)
            {
                this.CommsMetr.ErrorMessage = "Connection Failure: Please verify Server activity, DeviceID, and TCP settings.";
            }
            else if (e is TimeoutException)
            {
                this.CommsMetr.ErrorMessage = "Timeout Failure: Please assess connection integrity.";
            }
            else if (e is InvalidModbusRequestException)
            {
                this.CommsMetr.ErrorMessage = "Command Failure: Sent MODBUS Command is not structurally sound.";
            }
            else if (e is NotImplementedException)
            {
                this.CommsMetr.ErrorMessage = "Command Failure: Function Code is incompatible with Transport Type and/or NModbus Library.";
            }
            else
            {
                this.CommsMetr.ErrorMessage = "Unknown Error: " + e.Message;
            }

            this.CommsMetr.NumResponses++;
            this.CommsMetr.NumErrors++;

            enginePublisher.Error($"Modbus polling failed for device: {deviceID}.\nDetails: {this.CommsMetr.ErrorMessage}", e);
        }
    }
}