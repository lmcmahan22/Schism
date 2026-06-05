// <copyright file="ModbusInterpreter.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Modbus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Schiism.Core.Configuration.Enums;
    using Schiism.Core.Configuration.StateControl;

    /// <inheritdoc/>
    public class ModbusInterpreter
    {
        /// <inheritdoc/>
        public List<string> InterpretRegs(ConfigState config, List<ushort> rawData)
        {
            List<string> result = new List<string>();

            int regsPerValue = config.SelectedDataSize switch
            {
                DataSize.Bit32 => 2,
                DataSize.Bit64 => 4,
                _ => 1, // "Bit16"
            };

            int bitWidth = regsPerValue * 16;

            for (int i = 0; i < config.DataLength; i += regsPerValue)
            {
                if (i + regsPerValue - 1 >= rawData.Count)
                {
                    // Not eenough registers remaining, exit the loop.
                    break;
                }

                // Convert the data into a series of workable bytes
                List<byte> bytes = new List<byte>(regsPerValue * 2);
                for (int j = 0; j < regsPerValue; j++)
                {
                    ushort reg = rawData[i + j];

                    // Add high byte then low byte for each register to get original ordering from each register (MSB/Big Endian)
                    // (i.e. "0x1234" becomes [0x12], [0x34])
                    bytes.Add((byte)(reg >> 8));
                    bytes.Add((byte)(reg & 0xFF));
                }

                ApplyEndianTransformation(bytes, config.SelectedEndian);

                string formatted = FormatBytes(bytes.ToArray(), bitWidth, config.SelectedNumericBase, config.AsciiEnable);
                result.Add(new string(formatted));

                for (int pad = 1; pad < regsPerValue; pad++)
                {
                    result.Add(new string(string.Empty));
                }
            }

            return result;
        }

        private void ApplyEndianTransformation(List<byte> bytes, Endian selEndian)
        {
            switch (selEndian)
            {
                case Endian.LittleEndian:
                    bytes.Reverse();
                    break;
                case Endian.BigEndianSW:
                    SwapBytesWithinWords(bytes);
                    break;
                case Endian.LittleEndianSW:
                    bytes.Reverse();
                    SwapBytesWithinWords(bytes);
                    break;
                default:
                    break;
            }
        }

        private void SwapBytesWithinWords(List<byte> bytes)
        {
            for (int j = 0; j + 1 < bytes.Count; j += 2)
            {
                byte tmp = bytes[j];
                bytes[j] = bytes[j + 1];
                bytes[j + 1] = tmp;
            }
        }

        private string FormatBytes(byte[] bytes, int bitWidth, NumericBase numericBase, bool asciiEnable)
        {
            switch (numericBase)
            {
                case NumericBase.Integer:
                    byte[] le = [.. bytes.Reverse()];

                    return bitWidth switch
                    {
                        32 => BitConverter.ToInt32(le, 0).ToString(),
                        64 => BitConverter.ToInt64(le, 0).ToString(),
                        _ => BitConverter.ToInt16(le, 0).ToString(), // "16-Bit" or default
                    };

                case NumericBase.Hexadecimal:
                    {
                        ulong unsigned = ToUnsigned(bytes);
                        string hex = bitWidth switch
                        {
                            32 => "0x" + unsigned.ToString("X8"),
                            64 => "0x" + unsigned.ToString("X16"),
                            _ => "0x" + unsigned.ToString("X4"), // "16-Bit" or default
                        };

                        if (asciiEnable)
                        {
                            string ascii = Encoding.ASCII.GetString(bytes);
                            return "(" + ascii + ") " + hex;
                        }

                        return hex;
                    }

                case NumericBase.Binary:
                    {
                        ulong unsigned = ToUnsigned(bytes);

                        string bin = Convert.ToString((long)unsigned, 2).PadLeft(bitWidth, '0');
                        string spaced = Regex.Replace(bin, ".{4}", "$0 ").Trim();
                        return spaced;
                    }

                case NumericBase.Float:
                    {
                        if (bitWidth == 32)
                        {
                            byte[] lef = [.. bytes.Reverse()]; // BitConverter expects little-endian on typical platforms
                            float f = BitConverter.ToSingle(lef, 0);
                            return f.ToString();
                        }

                        if (bitWidth == 64)
                        {
                            byte[] led = [.. bytes.Reverse()];
                            double d = BitConverter.ToDouble(led, 0);
                            return d.ToString();
                        }

                        // 16-bit is not a valid floating point width; fall back (not even possible to hit this with current UI handling)
                        return "N/A";
                    }

                default: // Decimal (unsigned)
                    return ToUnsigned(bytes).ToString();
            }
        }

        private ulong ToUnsigned(byte[] bytes)
        {
            ulong value = 0;
            foreach (byte b in bytes)
            {
                value = value << 8 | b;
            }

            return value;
        }
    }
}
