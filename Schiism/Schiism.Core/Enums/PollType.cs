// <copyright file="PollType.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Enums
{
    /// <summary>
    /// Endian Enumeration defines the Endianness of the incoming data (transformation applied during data interpretation, which is after initial reciept of data).
    /// </summary>
    public enum PollType
    {
        /// <summary>
        /// Coil Status Digital Data (MODBUS address 0 to 65535, entered as 0 to 65535 in app UI).
        /// </summary>
        CoilStatus,

        /// <summary>
        /// Input Status Digital Data (MODBUS address 10000 to 165535, entered as 0 to 65535 in app UI).
        /// </summary>
        InputStatus,

        /// <summary>
        /// Holding Register Non-Digital Data (MODBUS address 400000 to 465535, entered as 0 to 65535 in app UI).
        /// </summary>
        HoldingRegisters,

        /// <summary>
        /// Input Register Non-Digital Data (MODBUS address 300000 to 365535, entered as 0 to 65535 in app UI).
        /// </summary>
        InputRegisters,
    }
}
