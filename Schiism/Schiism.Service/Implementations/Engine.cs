// <copyright file="Engine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions;
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
    public class Engine(IModbusConfig config, IModbusClient client, IModbusInterpreter interpreter, IStreamQueue<ModbusData> modbusStreamQueue, IStreamQueue<ConnectionDiagnostics> connStreamQueue, ILogger<Engine> logger) : IEngine
    {
        private int numRequests = 0;
        private int numResponses = 0;
        private int numOKs = 0;
        private int numErrors = 0;
        private bool isConnected = false;
        private string errorMessage = string.Empty;

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct)
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
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            this.isConnected = false; // Not reported, but set the flag to false to prevent any false reporting of connection status while disconnecting
            await client.DisconnectAsync();
            logger.LogInformation("Disconnected from Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
        }

        /// <inheritdoc/>
        public async Task PollOnceAsync(CancellationToken ct)
        {
            try
            {
                await this.OnRequest(ct);

                List<ushort> rawData = client.ReadData(config);

                if (rawData is null || rawData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus response");
                }

                List<string>? interp = config.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters
                    ? interpreter.InterpretRegs(config, rawData)
                    : rawData.Select(x => x.ToString()).ToList();

                // Enqueue the modbus data for downstream processing
                ModbusData data = new(config.DeviceId, interp, DateTime.UtcNow);
                await modbusStreamQueue.EnqueueAsync(data, ct);

                await this.OnSuccess(ct);
                logger.LogInformation("Successfully polled Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.OnError(ex, ct);
                logger.LogError(ex, "Unknown error from Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
            }
        }

        private async Task OnRequest(CancellationToken ct)
        {
            this.numRequests++;
            ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.isConnected, DateTime.UtcNow);
            await connStreamQueue.EnqueueAsync(diag, ct);
        }

        private async Task OnSuccess(CancellationToken ct)
        {
            this.isConnected = true;
            this.numResponses++;
            this.numOKs++;
            this.errorMessage = string.Empty;
            ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.isConnected, DateTime.UtcNow);
            await connStreamQueue.EnqueueAsync(diag, ct);
        }

        private async Task OnError(Exception ex, CancellationToken ct)
        {
            this.isConnected = false;
            this.numResponses++;
            this.numErrors++;
            this.errorMessage = this.MapError(ex);
            ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.isConnected, DateTime.UtcNow);
            await connStreamQueue.EnqueueAsync(diag, ct);
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