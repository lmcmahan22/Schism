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
        Task RestartAsync();

        /// <summary>
        /// RunAsync method defines what the Background Service will be executing on thread 0.
        /// </summary>
        /// <param name="token">
        /// Token defined as a means for stopping the application, when desired.
        /// </param>
        /// <returns>
        /// The Task returned is passed up to the Worker class that calls this method.
        /// </returns>
        Task RunAsync(CancellationToken token);
    }
}
