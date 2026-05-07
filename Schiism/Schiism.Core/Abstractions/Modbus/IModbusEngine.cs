// <copyright file="IModbusEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.Modbus
{
    /// <summary>
    /// Interface for running the MODBUS Engine.
    /// While the class doesn't need to be abstracted for multiple implementations, this is necessary for Dependency Injection in the Service class.
    /// </summary>
    public interface IModbusEngine
    {
        Task ConnectAsync(CancellationToken token);

        Task DisconnectAsync();

        Task PollOnceAsync(CancellationToken token);
    }
}
