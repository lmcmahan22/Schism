// <copyright file="EngineWorker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.HostedServices
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Configuration.StateControl;
    using Schiism.Core.IPC.StateWrappers;
    using Schiism.Core.Modbus;

    /// <summary>
    /// Worker class runs the MODBUS Engine in a background thread, allowing it to run independently of the main service thread and be restarted when necessary.
    /// </summary>
    public class ModbusEngineWorker(Engine engine, ConfigState config, PollControl pollControl, ILogger<ModbusEngineWorker> logger, IHostApplicationLifetime lifetime) : BackgroundService
    {

        private List<string> latestData = new List<string>();

        /// <summary>
        /// Executes the background service, managing the lifecycle of the MODBUS Engine.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Don't run this worker until the application is fully started (i.e. all three Worker's StartAsync() methods are complete).
            await Task.Run(
                () => lifetime.ApplicationStarted.WaitHandle.WaitOne(), ct);

            logger.LogInformation("Service Engine Worker started");

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
                    await RunPollingLoop(sessionCts.Token);
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
            }
        }

        private async Task RunPollingLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                latestData = await engine.PollOnceAsync(ct, latestData);

                if (pollControl.RestartRequested)
                {
                    pollControl.RestartRequested = false;
                    throw new OperationCanceledException("Restart requested");
                }

                // The only scanRate delay that we should need in this application.
                await Task.Delay(config.ScanRate, ct);
            }
        }
    }
}
