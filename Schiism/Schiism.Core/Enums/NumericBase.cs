// <copyright file="NumericBase.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using System.ComponentModel;

namespace Schiism.Core.Enums
{
    /// <summary>
    /// Numeric Base Enumeration defines the Numeric Base of the incoming data (transformation applied during data interpretation, which is after initial reciept of data).
    /// </summary>
    public enum NumericBase
    {
        /// <summary>
        /// Unsigned Base 10 (MODBUS TCP default): 0 to 65535 per 16 bit register.
        /// </summary>
        [Description("Decimal")]
        Decimal,

        /// <summary>
        /// Signed Base 10: -32768 to 32767 per 16 bit register.
        /// </summary>
        [Description("Integer")]
        Integer,

        /// <summary>
        /// Unsigned Base 16: 0x0000 to 0xFFFF per 16 bit register.
        /// </summary>
        [Description("Hexadecimal")]
        Hexadecimal,

        /// <summary>
        /// Unsigned Base 2: (0000 0000 0000 0000)2 to (1111 1111 1111 1111)2 per 16 bit register.
        /// </summary>
        [Description("Binary")]
        Binary,

        /// <summary>
        /// Floating Point: +/- 1.18 x 10^-38 to +/- 3.40 x 10^38 per 32 bit pair of registers (logic prevents user from configuring Float at 16-bits, since this is not possible).
        /// </summary>
        [Description("Float")]
        Float,
    }
}
