// <copyright file="ModbusInterpreter.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Schiism.Core.Models.Config;
    using Schiism.Core.Models.Enums;

    // Parsing and formatting logic goes here, after the client class has done its thing
    // Configure this to work with Coils as well. Currently you just have the register control.
    public class ModbusInterpreter
    {
        // Interpret the received register data, according to parameters received from the front end
        public List<string> InterpretRegs(List<ushort> rawData, ushort length, bool asciiEnable, DataSize selDataSize, NumericBase selNumericBase, Endian selEndian)
        {
            // Convert raw ushort registers into List<string> for UI display, applying user-selected transformations for data size, numeric base, endianness, and ASCII interpretation.
            var result = new List<string>();

            // Determine how many 16-bit registers compose one displayed value
            int regsPerValue = selDataSize switch
            {
                DataSize.Bit32 => 2,
                DataSize.Bit64 => 4,
                _ => 1, // "Bit16"
            };

            // Calculate total bit width for formatting purposes (16, 32, 64)
            int bitWidth = regsPerValue * 16;

            // Loop through the registers in chunks corresponding to the selected data size (1 register for 16-bit, 2 for 32-bit, 4 for 64-bit)
            for (int i = 0; i < length; i += regsPerValue)
            {
                if (i + regsPerValue - 1 >= rawData.Count)
                {
                    break; // not enough registers remaining, scaled based on registers per value
                }

                // Break the current chunk of registers into bytes in MSB-first order (per register).
                // For example if we're using 32-bit values (2 registers per value) [reg0, reg1], we get [reg0hi, reg0lo, reg1hi, reg1lo] for 4 total bytes.
                // The same occurs for 16-bit and 64-bit, just with different byte counts, 2 and 8 respectively.
                List<byte> bytes = new List<byte>(regsPerValue * 2);
                for (int j = 0; j < regsPerValue; j++)
                {
                    // i + j = the current value + the current register within that value
                    ushort reg = rawData[i + j];

                    // Add high byte then low byte for each register to get original ordering from each register (MSB/Big Endian)
                    bytes.Add((byte)(reg >> 8));
                    bytes.Add((byte)(reg & 0xFF));
                }

                // Apply endian transformation to the series of bytes acquired, based on the selected endian option.
                this.ApplyEndianTransformation(bytes, selEndian);

                // Format value according to data size, numeric base, and ASCII enable selection (Hex only)
                string formatted = this.FormatBytes(bytes.ToArray(), bitWidth, selNumericBase, asciiEnable);

                // Add result to the collection as a string, which the UI binds to for display
                result.Add(new string(formatted));

                // For multi-register values, add placeholder cells to keep display alignment
                for (int pad = 1; pad < regsPerValue; pad++)
                {
                    result.Add(new string(string.Empty));
                }
            }

            return result;
        }

        private void ApplyEndianTransformation(List<byte> bytes, Endian selEndian)
        {
            // bytes currently MSB-first per register: [reg0hi, reg0lo, reg1hi, reg1lo, ...]
            // Handle selected endian options.
            switch (selEndian)
            {
                case Endian.LittleEndian:
                    // Reverse full array: [a,b,c,d] -> [d,c,b,a]
                    bytes.Reverse();
                    break;
                case Endian.BigEndianSW:
                    // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
                    this.SwapBytesWithinWords(bytes);
                    break;
                case Endian.LittleEndianSW:
                    // Reverse full array then swap within each word: [a,b,c,d] -> [d,c,b,a] -> [c,d,a,b]
                    bytes.Reverse();
                    this.SwapBytesWithinWords(bytes);
                    break;
                default:
                    // "BigEndian" -> keep as-is
                    break;
            }
        }

        private void SwapBytesWithinWords(List<byte> bytes)
        {
            // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
            for (int j = 0; j + 1 < bytes.Count; j += 2)
            {
                byte tmp = bytes[j];
                bytes[j] = bytes[j + 1];
                bytes[j + 1] = tmp;
            }
        }

        private string FormatBytes(byte[] bytes, int bitWidth, NumericBase numericBase, bool asciiEnable)
        {
            // Interpret bytes as MSB-first when manually constructing integers.
            // For floating point we need little-endian byte[] for BitConverter on typical platforms,
            // so reverse when calling BitConverter for floats/doubles.
            switch (numericBase)
            {
                case NumericBase.Integer:
                    var le = bytes.Reverse().ToArray(); // BitConverter expects little-endian on typical platforms

                    // Use BitConverter to convert byte array to the appropriate integer type based on bit width, then convert to string for display.
                    return bitWidth switch
                    {
                        32 => BitConverter.ToInt32(le, 0).ToString(),
                        64 => BitConverter.ToInt64(le, 0).ToString(),
                        _ => BitConverter.ToInt16(le, 0).ToString(), // "16-Bit" or default
                    };

                case NumericBase.Hexadecimal:
                    {
                        // Convert byte array to an unsigned long for hex formatting, since hex is typically used for raw values regardless of signedness.
                        ulong unsigned = this.ToUnsigned(bytes);
                        string hex = bitWidth switch
                        {
                            32 => "0x" + unsigned.ToString("X8"),
                            64 => "0x" + unsigned.ToString("X16"),
                            _ => "0x" + unsigned.ToString("X4"), // "16-Bit" or default
                        };

                        // Append ASCII contents, if enabled by user.
                        if (asciiEnable)
                        {
                            // Show ASCII interpreted from the current byte order
                            // NOTE: Contents will vary, depending on how many characters are placed in each register by the server
                            string ascii = Encoding.ASCII.GetString(bytes);
                            return "(" + ascii + ") " + hex;
                        }

                        return hex;
                    }

                case NumericBase.Binary:
                    {
                        // Convert byte array to an unsigned long for binary formatting, since binary is typically used for raw values regardless of signedness.
                        ulong unsigned = this.ToUnsigned(bytes);

                        // Format binary with leading zeros based on bit width, and add spaces every 4 bits for readability.
                        string bin = Convert.ToString((long)unsigned, 2).PadLeft(bitWidth, '0');
                        string spaced = Regex.Replace(bin, ".{4}", "$0 ").Trim();
                        return spaced;
                    }

                case NumericBase.Float:
                    {
                        if (bitWidth == 32)
                        {
                            var lef = bytes.Reverse().ToArray(); // BitConverter expects little-endian on typical platforms
                            float f = BitConverter.ToSingle(lef, 0);
                            return f.ToString();
                        }

                        if (bitWidth == 64)
                        {
                            var led = bytes.Reverse().ToArray();
                            double d = BitConverter.ToDouble(led, 0);
                            return d.ToString();
                        }

                        // Not a valid floating point width; fall back (not even possible to hit this with current UI handling)
                        return "N/A";
                    }

                default: // Decimal (unsigned)
                    return this.ToUnsigned(bytes).ToString();
            }
        }

        private ulong ToUnsigned(byte[] bytes)
        {
            // Build unsigned integer from byte array
            ulong value = 0;
            foreach (var b in bytes)
            {
                // Shift existing value left by 8 bits and add the next byte, effectively concatenating the bytes together in MSB-first order, now that we know we have the bytes in the order that we want for display.
                value = value << 8 | b;
            }

            return value;
        }
    }
}
