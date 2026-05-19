// <copyright file="Endian.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.ComponentModel;

namespace Schiism.Core.Enums
{
    /// <summary>
    /// Endian Enumeration defines the Endianness of the incoming data (transformation applied during data interpretation, which is after initial reciept of data).
    /// </summary>
    public enum Endian
    {
        /// <summary>
        /// Big Endian (MODBUS TCP default): [a,b,c,d].
        /// </summary>
        [Description("Big Endian")]
        BigEndian,

        /// <summary>
        ///  Little Endian. // Reverse full array: [d,c,b,a] -> [a,b,c,d].
        /// </summary>
        [Description("Little Endian")]
        LittleEndian,

        /// <summary>
        ///  Big Endian (Byte-Swap) // Swap bytes within each 16-bit word: [b,a,d,c] -> [a,b,c,d].
        /// </summary>
        [Description("Big Endian (Byte Swap)")]
        BigEndianSW,

        /// <summary>
        ///  Little Endian (Byte-Swap) // Reverse full array then swap within each word: [c,d,a,b] -> [b,a,d,c] -> [a,b,c,d].
        /// </summary>
        [Description("Little Endian (Byte Swap)")]
        LittleEndianSW,
    }
}
