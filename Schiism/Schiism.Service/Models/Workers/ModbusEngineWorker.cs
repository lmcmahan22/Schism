// <copyright file="Worker.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Workers
{
    using Schiism.Core.Abstractions.Modbus;

    /// <summary>
    /// Worker class contains the background execution loop that will use your engine.
    /// WHAT DOES ASYNC AND AWAIT MEAN???.
    /// </summary>
    public class ModbusEngineWorker(IModbusEngine engine) : BackgroundService
    {
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await engine.RunAsync(stoppingToken);
        }
    }
}
