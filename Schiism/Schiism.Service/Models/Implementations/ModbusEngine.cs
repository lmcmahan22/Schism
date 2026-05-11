// <copyright file="ModbusEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations
{
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Abstractions.IPC.Streams;
    using Schiism.Core.Abstractions.Modbus;
    using Schiism.Core.Models.DTOs.IPC_Records.Streams;
    using Schiism.Core.Models.Enums;

    /// <summary>
    /// Main MODBUS Engine class.
    /// Queues the stream transmissions to the client, but doesn't actually send them from here.
    /// </summary>
    public class ModbusEngine(IModbusConfig config, IModbusClient client, IModbusInterpreter interpreter, IStreamQueue<ModbusData> modbusStreamQueue, IStreamQueue<ConnectionDiagnostics> connStreamQueue) : IModbusEngine
    {
        private int numRequests = 0;
        private int numResponses = 0;
        private int numOKs = 0;
        private int numErrors = 0;
        private bool isConnected = false; // IMPLEMENT!!!
        private string errorMessage = string.Empty;

        public async Task ConnectAsync(CancellationToken ct)
        {
            try
            {
                await this.OnRequest(ct);
                await client.InitializeAsync(config);
                await this.OnSuccess(ct);
            }
            catch (Exception e)
            {
                await this.OnError(e, ct);
            }
        }

        public async Task DisconnectAsync()
        {
            await client.DisconnectAsync();
        }

        public async Task PollOnceAsync(CancellationToken ct)
        {
            try
            {
                await this.OnRequest(ct);

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

                await this.OnSuccess(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.OnError(ex, ct);
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
            this.numResponses++;
            this.numOKs++;
            this.errorMessage = string.Empty;
            ConnectionDiagnostics diag = new(this.numRequests, this.numResponses, this.numOKs, this.numErrors, this.errorMessage, this.isConnected, DateTime.UtcNow);
            await connStreamQueue.EnqueueAsync(diag, ct);
        }

        private async Task OnError(Exception ex, CancellationToken ct)
        {
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