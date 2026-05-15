// <copyright file="ModbusEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Enums;
    using Schiism.Core.Models.IPC.DTOs.Streams;

    /// <summary>
    /// Main MODBUS Engine class.
    /// Queues the stream transmissions to the client, but doesn't actually send them from here.
    /// Additionally, this class does NOT loop! Ideally, I want to make it such that only my Workers contain loops, thereby making the cancellation token handling discrete.
    /// </summary>
    public class Engine(IModbusConfig config, IModbusClient client, IModbusInterpreter interpreter, IStreamQueue<ModbusData> modbusStreamQueue, IStreamQueue<ConnectionDiagnostics> connStreamQueue, ILogger<Engine> logger) : IEngine
    {
        private int numRequests = 0;
        private int numResponses = 0;
        private int numOKs = 0;
        private int numErrors = 0;
        private bool isConnected = false;
        private string errorMessage = string.Empty;

        public async Task ConnectAsync(CancellationToken ct)
        {
            try
            {
                logger.LogInformation("Attempting to connect to Modbus Server at {IP}:{Port} with timeout {Timeout}ms", config.IPAddress, config.TCPPort, config.TCPTimeout);
                await OnRequest(ct);
                await client.InitializeAsync(config);
                await OnSuccess(ct);
                logger.LogInformation("Successfully connected to Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
            }
            catch (Exception e)
            {
                await OnError(e, ct);
                logger.LogError(e, "Failed to connect to Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
            }
        }

        public async Task DisconnectAsync()
        {
            isConnected = false; // Not reported, but set the flag to false to prevent any false reporting of connection status while disconnecting
            await client.DisconnectAsync();
            logger.LogInformation("Disconnected from Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
        }

        public async Task PollOnceAsync(CancellationToken ct)
        {
            try
            {
                await OnRequest(ct);

                var rawData = client.ReadData(config);

                if (rawData is null || rawData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus response");
                }

                var interp = config.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters
                    ? interpreter.InterpretRegs(config, rawData)
                    : rawData.Select(x => x.ToString()).ToList();

                // Enqueue the modbus data for downstream processing
                ModbusData data = new(config.DeviceId, interp, DateTime.UtcNow);
                await modbusStreamQueue.EnqueueAsync(data, ct);

                await OnSuccess(ct);
                logger.LogInformation("Successfully polled Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
            }
            catch (SocketException ex)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "Failed to poll Modbus Server at {IP}:{Port} due to Socket Error. Attempting to reconnect...", config.IPAddress, config.TCPPort);
                throw;
            }
            catch (IOException ex)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "Failed to poll Modbus Server at {IP}:{Port} due to IO Error. Attempting to reconnect...", config.IPAddress, config.TCPPort);
                throw;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await OnError(ex, ct);
                logger.LogError(ex, "Unknown error from Modbus Server at {IP}:{Port}", config.IPAddress, config.TCPPort);
            }
        }

        private async Task OnRequest(CancellationToken ct)
        {
            numRequests++;
            ConnectionDiagnostics diag = new(numRequests, numResponses, numOKs, numErrors, errorMessage, isConnected, DateTime.UtcNow);
            await connStreamQueue.EnqueueAsync(diag, ct);
        }

        private async Task OnSuccess(CancellationToken ct)
        {
            isConnected = true;
            numResponses++;
            numOKs++;
            errorMessage = string.Empty;
            ConnectionDiagnostics diag = new(numRequests, numResponses, numOKs, numErrors, errorMessage, isConnected, DateTime.UtcNow);
            await connStreamQueue.EnqueueAsync(diag, ct);
        }

        private async Task OnError(Exception ex, CancellationToken ct)
        {
            isConnected = false;
            numResponses++;
            numErrors++;
            errorMessage = MapError(ex);
            ConnectionDiagnostics diag = new(numRequests, numResponses, numOKs, numErrors, errorMessage, isConnected, DateTime.UtcNow);
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