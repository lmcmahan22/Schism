// <copyright file="ModbusControl.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.Modbus
{
    using Schiism.Core.Abstractions.Modbus;

    /// <inheritdoc/>
    public class ModbusControl : IModbusControl
    {
        /// <inheritdoc/>
        public bool RestartRequested { get; set; }
    }
}
