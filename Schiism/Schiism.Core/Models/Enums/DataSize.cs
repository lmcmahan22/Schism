// <copyright file="DataSize.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Enums
{
    /// <summary>
    /// DataSize Enumeration defines how many bits (and therefore how many registers) defines a single piece of MODBUS data for the user.
    /// </summary>
    public enum DataSize
    {
        /// <summary>
        /// 16 Bits (1 register) per data point.
        /// </summary>
        Bit16,

        /// <summary>
        /// 32 Bits (2 registers) per data point.
        /// </summary>
        Bit32,

        /// <summary>
        /// 64 Bits (4 registers) per data point.
        /// </summary>
        Bit64,
    }
}
