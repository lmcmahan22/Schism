// <copyright file="Worker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.Modbus;

    /// <summary>
    /// Worker class runs the MODBUS Engine in a background thread, allowing it to run independently of the main service thread and be restarted on demand.
    /// </summary>
    public class ModbusEngineWorker(IEngine engine, IModbusConfig config, IModbusControl modbusControl, ILogger<ModbusEngineWorker> logger) : BackgroundService
    {
        private CancellationTokenSource? sessionCts;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                try
                {
                    await engine.ConnectAsync(sessionCts.Token);

                    await RunPollingLoop(sessionCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    logger.LogInformation("Modbus Engine session restarted");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Modbus Engine failure: {ex}");
                }
                finally
                {
                    await engine.DisconnectAsync();
                }

                await Task.Delay(config.ScanRate, ct);
            }
        }

        private async Task RunPollingLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // if (!engine.IsConnected)
                // {
                //    throw new OperationCanceledException("Client disconnected");
                // }

                await engine.PollOnceAsync(ct);

                if (modbusControl.RestartRequested)
                {
                    modbusControl.ClearRestartRequest();
                    logger.LogInformation("Modbus restart requested");
                    throw new OperationCanceledException("Restart requested");
                }

                await Task.Delay(config.ScanRate, ct);
            }
        }
    }
}
