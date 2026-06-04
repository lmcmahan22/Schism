// <copyright file="IModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.Modbus
{
    using Schiism.Core.Abstractions.IPC.States;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for defining what the TCP Client will be doing.
    /// </summary>
    public interface IModbusClient
    {
        /// <summary>
        /// Initialize the TCP Client connection to the server device using the settings defined in the ModbusConfig object.
        /// </summary>
        /// <param name="mC">
        /// ModbusConfig object. Necessary for initializing the TCP client connection.
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(IConfigState mC);

        /// <summary>
        /// Disconnects the TCP Client from the server device.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Unified method for handling how to poll MODBUS data from the server device for all data types (i.e. Status Coils, Status Inputs, Holding Regsiters, and Input Registers).
        /// </summary>
        /// <param name="mC">
        /// ModbusConfig object containing everything that the Client needs to know for polling MODBUS data from the server.
        /// </param>
        /// <returns>
        /// MODBUS data is returned as a List of ushorts (0-65535) with one uShort for each coil/register, regardless of configuration settings.
        /// </returns>
        List<ushort> ReadData(IConfigState mC);

        List<ushort> ReadCoilData(IConfigState mC);

        List<ushort> ReadRegisterData(IConfigState mC);
    }
}