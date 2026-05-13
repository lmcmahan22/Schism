// <copyright file="Endian.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

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
        BigEndian,

        /// <summary>
        ///  Little Endian. // Reverse full array: [d,c,b,a] -> [a,b,c,d].
        /// </summary>
        LittleEndian,

        /// <summary>
        ///  Big Endian (Byte-Swap) // Swap bytes within each 16-bit word: [b,a,d,c] -> [a,b,c,d].
        /// </summary>
        BigEndianSW,

        /// <summary>
        ///  Little Endian (Byte-Swap) // Reverse full array then swap within each word: [c,d,a,b] -> [b,a,d,c] -> [a,b,c,d].
        /// </summary>
        LittleEndianSW,
    }
}
