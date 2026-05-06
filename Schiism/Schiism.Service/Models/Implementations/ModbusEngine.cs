// <copyright file="ModbusEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations
{
    using NModbus;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Logging;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.DTOs;
    using Schiism.Core.Models.DTOs.IPC.Streams;
    using Schiism.Core.Models.Enums;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Main MODBUS Engine class.
    /// Implements EngineService and uses the following via Dependency Injection:
    ///     - client
    ///     - ModbusInterpreter
    ///     - DataPublisher
    ///     - EnginePublisher
    ///     - NOTE: Including these at the top of the class removes the need for an explicit constructor! These are readonly!).
    /// Contains its own instance of commsics, which the Engine only has context to at the top level (here).
    /// </summary>
    public class ModbusEngine(IModbusConfig config, ConnectionDiagnostics comms, IModbusClient client, IModbusInterpreter interpreter, IDataPublisher dataPublisher, IEnginePublisher enginePublisher, IStreamQueue<ModbusData> modbusStreamQueue, IStreamQueue<ConnectionDiagnostics> connDiagStreamQueue) : IModbusEngine
    {

        private readonly SemaphoreSlim lifecycleLock = new(1, 1);
        private CancellationTokenSource? engineCts;
        private Task? runTask;

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken token)
        {
            if (config == null)
            {
                throw new InvalidOperationException("Engine not configured.");
            }

            await client.ConnectAsync(config);

            while (!token.IsCancellationRequested)
            {
                await PollData(config, token);
                await Task.Delay(config.ScanRate, token);
            }
        }

        private async Task PollData(IModbusConfig config, CancellationToken ct)
        {
            await Task.Yield();

            enginePublisher.Info($"Attempting to poll device {config.DeviceId} at {config.IPAddress}:");

            while (!ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    RequestInc(ct);

                    // if (client?.IsConnected != true)
                    // {
                    //    throw new InvalidOperationException("Connection lost.");
                    // }
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

                    // Enqueue raw data and interpreted data for streaming to UI and other potential subscribers.
                    ModbusData modbusData = new ModbusData(config.DeviceId, interp, DateTime.UtcNow);
                    await modbusStreamQueue.EnqueueAsync(modbusData, ct);

                    comms.IsConnected = true;
                    SuccessResp(ct);

                    await Task.Delay(config.ScanRate, ct);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    comms.IsConnected = false;
                    FailResp(e, config.DeviceId, ct);
                }
            }
        }

        private async void RequestInc(CancellationToken ct)
        {
            comms.NumRequests++;

            // Enqueue diagnostic data, since this has been updated.
            await connDiagStreamQueue.EnqueueAsync(comms, ct);
        }

        private async void SuccessResp(CancellationToken ct)
        {
            comms.NumResponses++;
            comms.NumOKs++;
            comms.ErrorMessage = string.Empty;

            // Enqueue diagnostic data, since this has been updated.
            await connDiagStreamQueue.EnqueueAsync(comms, ct);
        }

        private async void FailResp(Exception e, byte deviceID, CancellationToken ct)
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

                comms.ErrorMessage = "Command Failure: Server did not accept the received MODBUS Command. Error Code: " + se.SlaveExceptionCode + " - \"" + details + "\"";
            }
            else if (e is IOException or SocketException)
            {
                comms.ErrorMessage = "Connection Failure: Please verify Server activity, DeviceID, and TCP settings.";
            }
            else if (e is TimeoutException)
            {
                comms.ErrorMessage = "Timeout Failure: Please assess connection integrity.";
            }
            else if (e is InvalidModbusRequestException)
            {
                comms.ErrorMessage = "Command Failure: Sent MODBUS Command is not structurally sound.";
            }
            else if (e is NotImplementedException)
            {
                comms.ErrorMessage = "Command Failure: Function Code is incompatible with Transport Type and/or NModbus Library.";
            }
            else
            {
                comms.ErrorMessage = "Unknown Error: " + e.Message;
            }

            comms.NumResponses++;
            comms.NumErrors++;

            enginePublisher.Error($"Modbus polling failed for device: {deviceID}.\nDetails: {comms.ErrorMessage}", e);

            // Enqueue diagnostic data, since this has been updated.
            await connDiagStreamQueue.EnqueueAsync(comms, ct);
        }

        public async Task RestartAsync()
        {
            await lifecycleLock.WaitAsync();
            try
            {
                await StopInternalAsync();
                await StartInternalAsync();
            }
            finally
            {
                lifecycleLock.Release();
            }
        }

        private Task StartInternalAsync()
        {
            engineCts = new CancellationTokenSource();

            runTask = Task.Run(() => RunAsync(engineCts.Token));

            return Task.CompletedTask;
        }

        private async Task StopInternalAsync()
        {
            if (engineCts == null)
                return;

            try
            {
                engineCts.Cancel();

                if (runTask != null)
                    await runTask;
            }
            catch (OperationCanceledException e)
            {
                // Expected...?

                // comms.ErrorMessage = "Settings Update Failure: Polling could not be stopped and operation was cancelled.";
                // enginePublisher.Error($"Modbus polling failed for device: {config.DeviceId}.\nDetails: {comms.ErrorMessage}", e);
                // // Enqueue diagnostic data, since this has been updated.
                // await connDiagStreamQueue.EnqueueAsync(comms, engineCts.Token);
            }
            finally
            {
                engineCts.Dispose();
                engineCts = null;
                runTask = null;
            }

            await client.DisconnectAsync();
        }
    }
}