// <copyright file="Engine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Modbus
{
    using Microsoft.Extensions.Logging;
    using NModbus;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.DTOs;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.IPC.Streams;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    /// <summary>
    /// Implemnting class for the IEngine interface.
    /// No looping or scheduling done in this class, only singular connect, disconnect, and polling attempts.
    /// </summary>
    public class Engine(ILogger<Engine> logger, ConfigState config, ModbusClient client, InitStatus initStatus, ModbusInterpreter interpreter, StreamQueue<ModbusDataCollectionDTO> modbusSQ, StreamQueue<ConnDiagDTO> connSQ)
    {
        private int numRequests = 0;
        private int numResponses = 0;
        private int numOKs = 0;
        private int numErrors = 0;
        private string errorMessage = string.Empty;
        private DateTime lastDiagTS = DateTime.UtcNow;

        // Referenced by the worker
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct)
        {
            while (!IsConnected)
            {
                try
                {
                    logger.LogInformation("[CORE] Service Client attempting to connect to Modbus Server at {IP}:{Port} with timeout {Timeout}ms", config.IPAddress, config.TCPPort, config.TCPTimeout);
                    await OnRequest(ct);
                    await client.InitializeAsync(config);

                    await OnSuccess(ct);
                    logger.LogInformation("[CORE] Service Client successfully connected to Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
                }
                catch (Exception e)
                {
                    await OnError(e, ct);
                    logger.LogError(e, "[CORE] Service Client Failed to connect to Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
                }

                await Task.Delay(config.ScanRate, ct); // Wait scan rate before retrying connection if it failed. This prevents spamming connection attempts in case of persistent failure.
            }
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            IsConnected = false; // Not reported, but set the flag to false to prevent any false reporting of connection status while disconnecting
            await client.DisconnectAsync();
            logger.LogInformation("[CORE] Disconnected from Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
        }

        /// <inheritdoc/>
        public async Task<List<string>> PollOnceAsync(CancellationToken ct, List<string> prevData)
        {
            ModbusDataCollectionDTO? modbusDTO = null;

            try
            {
                await OnRequest(ct);

                // Poll both types of data, since the PC may need to know information from the PLC that the user isn't observing at this moment (ex. User is observing Part Name string while SMEMA gets updated).
                List<ushort> rawCoilData = await client.ReadCoilDataAsync(config);

                if (rawCoilData is null || rawCoilData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus Coil response");
                }

                List<ushort> rawRegisterData = await client.ReadRegisterDataAsync(config);

                if (rawRegisterData is null || rawRegisterData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus Register response");
                }

                logger.LogInformation("[CORE] [STREAM] Received MODBUS Data from Server.");

                List<string>? interp;

                // Only attmpt to queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
                if (initStatus.IsInitialized)
                {

                    if (config.SelectedPollType is PollType.HoldingRegisters)
                    {
                        // Data needs to be interpretted, according to config settings
                        interp = interpreter.InterpretRegs(config, rawRegisterData);
                    }
                    else
                    {
                        // No interpretation needed for coils, just convert to string for uniformity in the frontend
                        interp = rawCoilData.Select(x => x.ToString()).ToList();
                    }

                    // Enqueue the modbus data for downstream processing
                    // Only enqueue and publish to UI if we've identified a purposeful difference in the data, regardless of the scan rate!
                    if (!Enumerable.SequenceEqual(interp, prevData))
                    {
                        modbusDTO = new(config.DeviceId, interp, DateTime.UtcNow);

                        // Put this back in if you need to see the data leaving the Service app
                        // logger.LogWarning("[CORE] Enqueuing ModbusData: {x}", string.Join(", ", modbusDTO.Data));

                        await modbusSQ.EnqueueAsync(modbusDTO, ct);
                    }
                }

                // Regardless of whether we queued up the last stream contents or not, this was still a successful poll, so we update the diagnostics and connection status accordingly.
                await OnSuccess(ct);
                // logger.LogInformation("[CORE] Successfully polled Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);

                // Pass up the enqueued data. The DTO will be null if nothing was sent, which is okay.
                if (modbusDTO is null)
                {
                    return prevData;
                }
                else
                {
                    return modbusDTO.Data;
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning("[CORE] Operation cancelled from Client Engine: {ex}", ex.Message);
                throw;
            }
            catch (SocketException ex)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "[CORE] Failed to poll Modbus Server at {IP}:{Port} due to Socket Error. Attempting to reconnect... Error details: {ex}", config.IPAddress, config.TCPPort, ex);
                throw;
            }
            catch (IOException ex)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "[CORE] Failed to poll Modbus Server at {IP}:{Port} due to IO Error. Attempting to reconnect... Error details: {ex}", config.IPAddress, config.TCPPort, ex);
                throw;
            }
            catch (ArgumentException ex)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "[CORE] Failed to poll Modbus Server at {IP}:{Port} due to Argument Error. Check configuration. Attempting to reconnect... Error details: {ex}", config.IPAddress, config.TCPPort, ex);
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "[CORE] Unknown error from Modbus Server at {IP}:{Port}... Error details: {ex}", config.IPAddress, config.TCPPort, ex);
                throw;
            }
        }

        public async Task PLCHeartbeatAsync(CancellationToken stoppingToken, ConfigState config)
        {
            await client.Heartbeat(stoppingToken, config);
        }

        public async Task WriteValueAsync(ModbusWriteDTO write, ConfigState config)
        {
            await client.WriteValue(write, config);
        }

        public async Task WriteBoardAvailableAsync(BoardAvailableDTO baDTO, ConfigState config)
        {
            await client.WriteBoardAvailableAsync(baDTO, config);
        }

        private async Task OnRequest(CancellationToken ct)
        {
            numRequests++;

            // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
            int diff = (int)(DateTime.UtcNow - this.lastDiagTS).TotalMilliseconds;

            // Not needed for now
            //logger.LogInformation("[CORE] Time since last Request ConnDiag send: {x}", diff);

            if (initStatus.IsInitialized && (diff > 500))
            {
                ConnDiagDTO diag = new(numRequests, numResponses, numOKs, numErrors, errorMessage, IsConnected, DateTime.UtcNow);
                lastDiagTS = diag.Timestamp;

                // Enqueue the ConnectionDiagnostics
                // logger.LogWarning("[CORE] Enqueuing Request ConnDiags: {x}", diag);
                await connSQ.EnqueueAsync(diag, ct);
            }
        }

        private async Task OnSuccess(CancellationToken ct)
        {
            IsConnected = true;
            numResponses++;
            numOKs++;
            errorMessage = string.Empty;

            // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
            int diff = (int)(DateTime.UtcNow - this.lastDiagTS).TotalMilliseconds;
            // logger.LogInformation("[CORE] Time since last Response ConnDiag send: {x}", diff);

            if (initStatus.IsInitialized && (diff > 500))
            {
                ConnDiagDTO diag = new(numRequests, numResponses, numOKs, numErrors, errorMessage, IsConnected, DateTime.UtcNow);
                lastDiagTS = diag.Timestamp;

                // Enqueue the ConnectionDiagnostics
                // logger.LogError($"Client is sending server Connection Status: {this.IsConnected}");
                // logger.LogWarning("[CORE] Enqueuing Response ConnDiags: {x}", diag);
                await connSQ.EnqueueAsync(diag, ct);
            }
        }

        private async Task OnError(Exception ex, CancellationToken ct)
        {
            IsConnected = false;
            numResponses++;
            numErrors++;
            errorMessage = MapError(ex);

            // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
            int diff = (int)(DateTime.UtcNow - this.lastDiagTS).TotalMilliseconds;
            // logger.LogInformation("[CORE] Time since last Response ConnDiag send: {x}", diff);

            if (initStatus.IsInitialized && (diff > 500))
            {
                ConnDiagDTO diag = new(numRequests, numResponses, numOKs, numErrors, errorMessage, IsConnected, DateTime.UtcNow);

                // logger.LogError($"Client is sending server Connection Status: {this.IsConnected}");
                // logger.LogWarning("[CORE] Enqueuing Response ConnDiags: {x}", diag);
                await connSQ.EnqueueAsync(diag, ct);
            }
        }

        private string MapError(Exception ex)
        {
            return ex switch
            {
                SlaveException se => $"MODBUS error {se.SlaveExceptionCode}",
                IOException ie => $"IO Connection failure {ie.Message}",
                SocketException soe => $"Socket Connection failure {soe.Message}",
                TimeoutException => "Timeout failure",
                _ => $"Unknown error: {ex.Message}",
            };
        }
    }
}