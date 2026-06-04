// <copyright file="Engine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Streams;

    /// <summary>
    /// Implemnting class for the IEngine interface.
    /// No looping or scheduling done in this class, only singular connect, disconnect, and polling attempts.
    /// </summary>
    /// <param name="config">The ModbusConfig object, DI'd.</param>
    /// <param name="client">The Modbus client object, DI'd.</param>
    /// <param name="interpreter">The Modbus interpreter object, DI'd.</param>
    /// <param name="modbusStreamQueue">The Modbus data stream queue, DI'd.</param>
    /// <param name="connStreamQueue">The connection diagnostics stream queue, DI'd.</param>
    /// <param name="logger">The logger object, DI'd.</param>
    public class Engine(IConfigState config, IModbusClient client, IModbusInterpreter interpreter, IStreamQueue<ModbusData> modbusStreamQueue, IStreamQueue<ConnectionDiagnostics> connStreamQueue, IInitializedState fEInitState, ILogger<Engine> logger) : IEngine
    {
        private int numRequests = 0;
        private int numResponses = 0;
        private int numOKs = 0;
        private int numErrors = 0;
        private string errorMessage = string.Empty;

        // Referenced by the worker
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct)
        {
            while (!this.IsConnected)
            {
                try
                {
                    logger.LogInformation("Attempting to connect to Modbus Server at {IP}:{Port} with timeout {Timeout}ms", config.IPAddress, config.TCPPort, config.TCPTimeout);
                    await this.OnRequest(ct);
                    await client.InitializeAsync(config);

                    await this.OnSuccess(ct);
                    logger.LogInformation("Successfully connected to Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
                }
                catch (Exception e)
                {
                    await this.OnError(e, ct);
                    logger.LogError(e, "Failed to connect to Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
                }

                await Task.Delay(1000, ct); // Wait one second before retrying connection if it failed. This prevents spamming connection attempts in case of persistent failure.
            }
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            this.IsConnected = false; // Not reported, but set the flag to false to prevent any false reporting of connection status while disconnecting
            await client.DisconnectAsync();
            logger.LogInformation("Disconnected from Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
        }

        /// <inheritdoc/>
        public async Task PollOnceAsync(CancellationToken ct)
        {
            try
            {
                await this.OnRequest(ct);

                // Poll both types of data for logging, only queue and publish selected for frontend.
                List<ushort> rawCoilData = client.ReadCoilData(config);
                List<ushort> rawRegisterData = client.ReadRegisterData(config);

                if (rawCoilData is null || rawCoilData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus Coil response");
                }

                if (rawRegisterData is null || rawRegisterData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus Register response");
                }

                List<string>? interp;
                ModbusData data;

                // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
                if (fEInitState.IsInitialized)
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
                    data = new(config.DeviceId, interp, DateTime.UtcNow);
                    await modbusStreamQueue.EnqueueAsync(data, ct);
                }

                // Regardless of whether we queued up the last stream contents or not, this was still a successful poll, so we update the diagnostics and connection status accordingly.
                await this.OnSuccess(ct);
                logger.LogInformation("Successfully polled Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning("Connection cancelled on Client Engine.");
                throw;
            }
            catch (SocketException ex)
            {
                await this.OnError(ex, ct);
                logger.LogError(ex, "Failed to poll Modbus Server at {IP}:{Port} due to Socket Error. Attempting to reconnect...", config.IPAddress, config.TCPPort);
                throw;
            }
            catch (IOException ex)
            {
                await this.OnError(ex, ct);
                logger.LogError(ex, "Failed to poll Modbus Server at {IP}:{Port} due to IO Error. Attempting to reconnect...", config.IPAddress, config.TCPPort);
                throw;
            }
            catch (ArgumentException ex)
            {
                await this.OnError(ex, ct);
                logger.LogError(ex, "Failed to poll Modbus Server at {IP}:{Port} due to Argument Error. Check configuration. Attempting to reconnect...", config.IPAddress, config.TCPPort);
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.OnError(ex, ct);
                logger.LogError(ex, "Unknown error from Modbus Server at {IP}:{Port} --> {ex}", config.IPAddress, config.TCPPort, ex);
            }
        }

        private async Task OnRequest(CancellationToken ct)
        {
            this.numRequests++;

            // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
            if (fEInitState.IsInitialized)
            {
                ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.IsConnected, DateTime.UtcNow);
                await connStreamQueue.EnqueueAsync(diag, ct);
            }
        }

        private async Task OnSuccess(CancellationToken ct)
        {
            this.IsConnected = true;
            this.numResponses++;
            this.numOKs++;
            this.errorMessage = string.Empty;

            // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
            if (fEInitState.IsInitialized)
            {
                ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.IsConnected, DateTime.UtcNow);

                // logger.LogError($"Client is sending server Connection Status: {this.IsConnected}");
                await connStreamQueue.EnqueueAsync(diag, ct);
            }
        }

        private async Task OnError(Exception ex, CancellationToken ct)
        {
            this.IsConnected = false;
            this.numResponses++;
            this.numErrors++;
            this.errorMessage = this.MapError(ex);

            // Only queue up the stream contents if the frontend has initialized. Otherwise, we would clog the stream until it starts up.
            if (fEInitState.IsInitialized)
            {
                ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.IsConnected, DateTime.UtcNow);

                // logger.LogError($"Client is sending server Connection Status: {this.IsConnected}");
                await connStreamQueue.EnqueueAsync(diag, ct);
            }
        }

        private string MapError(Exception ex)
        {
            return ex switch
            {
                SlaveException se => $"MODBUS error {se.SlaveExceptionCode}",
                IOException => "Connection failure",
                SocketException => "Connection failure",
                TimeoutException => "Timeout failure",
                _ => $"Unknown error: {ex.Message}",
            };
        }
    }
}