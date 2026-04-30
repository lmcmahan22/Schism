// <copyright file="IModbusInterpreter.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Handlers;

    /// <summary>
    /// Interface for defining the MODBUS data interpreter, which executes discretely from the TCP client.
    /// </summary>
    public interface IModbusInterpreter
    {
        /// <summary>
        /// Interpret logic is only needed on registers, since the ushort data containing a non-digital value could have various meanings, all according to the modbus configuration.
        /// </summary>
        /// <param name="mC">
        /// ModbusConfig object containing everything that the Client needs to know for polling MODBUS data from the server.
        /// </param>
        /// <param name="rawData">
        /// Ushort List received from the MODBUS TCP Client object.
        /// </param>
        /// <returns>
        /// Returns a List of strings defining the interpreted data (ex. ushorts become a collection of Hex based ASCII characters).
        /// </returns>
        List<string> InterpretRegs(IModbusConfig mC, List<ushort> rawData);
    }
}
