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
    using Schiism.Core.Models.DTOs.IPC_Records.Streams;
    using Schiism.Core.Models.Enums;
    using Schiism.Service.Models.Implementations.Modbus;
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
    public class ModbusEngine(IModbusConfig config, IModbusClient client, IModbusInterpreter interpreter, IDataPublisher dataPublisher, IEnginePublisher enginePublisher, IStreamQueue<ModbusData> modbusStreamQueue, IStreamQueue<ConnectionDiagnostics> connDiagStreamQueue) : IModbusEngine
    {
        private readonly IModbusDiagnosticsTracker diagnostics = new ModbusDiagnosticsTracker();

        public async Task ConnectAsync(CancellationToken ct)
        {
            try
            {
                await client.InitializeAsync(config);
                diagnostics.OnSuccess();
                enginePublisher.Info($"Successfully initialized Modbus device: {config.DeviceId} at {config.IPAddress}:{config.TCPPort}.");
            }
            catch (Exception e)
            {
                diagnostics.OnError(e);
                enginePublisher.Error($"Failed to connect to Modbus device: {config.DeviceId} at {config.IPAddress}:{config.TCPPort}.\nDetails: {diagnostics.Snapshot().ErrorMessage}", e);
            }
            finally
            {
                // Enqueue diagnostic data, since this has been updated.
                await connDiagStreamQueue.EnqueueAsync(diagnostics.Snapshot(), ct);
            }
        }

        public async Task DisconnectAsync()
        {
            await client.DisconnectAsync();
            diagnostics.OnSuccess();
            enginePublisher.Info($"Disconnected from Modbus device: {config.DeviceId} at {config.IPAddress}:{config.TCPPort}.");
        }

        public async Task PollOnceAsync(CancellationToken ct)
        {
            try
            {
                diagnostics.OnRequest();

                var rawData = client.ReadData(config);

                if (rawData is null || rawData.Count != config.DataLength)
                {
                    throw new Exception("Invalid Modbus response");
                }

                var interp = config.SelectedPollType is PollType.HoldingRegisters or PollType.InputRegisters
                    ? interpreter.InterpretRegs(config, rawData)
                    : rawData.Select(x => x.ToString()).ToList();

                // Data stream handles this now.
                // var snap = new DataSnapshotDto
                // {
                //    Data = interp,
                //    DeviceId = config.DeviceId,
                // };

                // dataPublisher.PublishData(snap);

                await modbusStreamQueue.EnqueueAsync(
                    new ModbusData(config.DeviceId, interp, DateTime.UtcNow),
                    ct);

                diagnostics.OnSuccess();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                diagnostics.OnError(ex);
                enginePublisher.Error($"Error occured while polling Modbus device: {config.DeviceId} at {config.IPAddress}:{config.TCPPort}.\nDetails: {diagnostics.Snapshot().ErrorMessage}", ex);
            }
            finally
            {
                await connDiagStreamQueue.EnqueueAsync(diagnostics.Snapshot(), ct);
            }
        }
    }
}