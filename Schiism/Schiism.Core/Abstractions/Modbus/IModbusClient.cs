// <copyright file="IModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.Modbus
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for defining what the TCP Client will be doing.
    /// </summary>
    public interface IModbusClient
    {

        Task InitializeAsync(IModbusConfig mC);

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
        List<ushort> ReadData(IModbusConfig mC);
    }
}