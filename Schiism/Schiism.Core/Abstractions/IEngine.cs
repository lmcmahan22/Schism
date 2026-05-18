// <copyright file="IEngine.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    /// <summary>
    /// Interface for running the MODBUS Engine.
    /// While the class doesn't need to be abstracted for multiple implementations, this is necessary for Dependency Injection in the Service class.
    /// </summary>
    public interface IEngine
    {
        /// <summary>
        /// Asynchronous connect method to establish connection with the Modbus device.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous connect operation.</returns>
        Task ConnectAsync(CancellationToken token);

        /// <summary>
        /// Asynchronous disconnect method to terminate connection with the Modbus device.
        /// </summary>
        /// <returns>A task that represents the asynchronous disconnect operation.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Asynchronous method to perform a single poll operation with the Modbus device.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous poll operation.</returns>
        Task PollOnceAsync(CancellationToken token);
    }
}
