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
    public class EngineWorker(IEngine engine, IModbusConfig config, IModbusControl modbusControl, ILogger<EngineWorker> logger) : BackgroundService
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
                catch (TaskCanceledException)
                {
                    logger.LogInformation("Modbus Server has closed. Attempting Reconnect...");
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Modbus Engine session restarted. Attempting Reconnect...");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Modbus Engine failure");
                    await engine.DisconnectAsync();
                }

                // finally
                // {
                //    await engine.DisconnectAsync();
                // }

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
