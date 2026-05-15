// <copyright file="IModbusControl.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.Modbus
{
    /// <summary>
    /// Used for tracking when an Engine restarted is needed.
    /// </summary>
    public interface IModbusControl
    {
        /// <summary>
        /// Gets or Sets a value indicating whether an engine restart is requested or not.
        /// </summary>
        bool RestartRequested { get; set; }
    }
}
