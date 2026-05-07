// <copyright file="Worker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Workers
{
    using Microsoft.Extensions.Logging;
    using Schiism.Core.Abstractions.Modbus;

    /// <summary>
    /// Worker class runs the MODBUS Engine in a background thread, allowing it to run independently of the main service thread and be restarted on demand.
    /// </summary>
    public class ModbusEngineWorker(IModbusEngine engine, ILogger<ModbusEngineWorker> logger) : BackgroundService
    {
        private CancellationTokenSource? sessionCts;

        public async Task RequestRestart()
        {
            sessionCts?.Cancel();
            await Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                try
                {
                    await engine.ConnectAsync(sessionCts.Token);
                    logger.LogInformation("Modbus Engine Connected");

                    await RunPollingLoop(sessionCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    logger.LogInformation("Modbus Engine session restarted");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Modbus Engine failure");
                }
                finally
                {
                    await engine.DisconnectAsync();
                }

                await Task.Delay(3000, ct);
            }
        }

        private async Task RunPollingLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await engine.PollOnceAsync(ct);
            }
        }
    }
}
