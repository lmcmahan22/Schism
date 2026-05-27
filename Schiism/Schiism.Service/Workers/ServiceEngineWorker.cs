// <copyright file="EngineWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Workers
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Abstractions.IPC.States;
    using Schiism.Core.Abstractions.Modbus;

    /// <summary>
    /// Worker class runs the MODBUS Engine in a background thread, allowing it to run independently of the main service thread and be restarted when necessary.
    /// </summary>
    public class ServiceEngineWorker(IEngine engine, IConfigState config, IModbusControl modbusControl, ILogger<ServiceEngineWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Executes the background service, managing the lifecycle of the MODBUS Engine.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                try
                {
                    logger.LogInformation("Connecting to Modbus engine...");
                    await engine.ConnectAsync(sessionCts.Token);

                    if (!engine.IsConnected)
                    {
                        throw new InvalidOperationException("Engine reported not connected after ConnectAsync");
                    }

                    logger.LogInformation("Connected. Starting polling loop.");
                    await this.RunPollingLoop(sessionCts.Token);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    logger.LogInformation("Modbus Engine session lost/restarted. Attempting Reconnect...");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Modbus Engine failure");
                    await engine.DisconnectAsync();
                }
                finally
                {
                    if (!engine.IsConnected) {
                        logger.LogError("Disconnecting from MODBUS server (if connected prior)...");
                        await engine.DisconnectAsync();
                    }
                }

                // This shouldn't need to be here, since connect and pollingloop both have their own delays.
                // await Task.Delay(config.ScanRate, ct);
            }
        }

        private async Task RunPollingLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await engine.PollOnceAsync(ct);

                if (modbusControl.RestartRequested)
                {
                    modbusControl.RestartRequested = false;
                    throw new OperationCanceledException("Restart requested");
                }

                // The only scanRate delay that we should need in this application.
                await Task.Delay(config.ScanRate, ct);
            }
        }
    }
}
