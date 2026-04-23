namespace Schiism.Core.Engine
{
    using NModbus;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Domain;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class ModbusEngineCore
    {

        // Ryan showed you a way to incorporate these not as instances, but as parameters from interfaces. You have the second part right...)
        private readonly IModbusClient modbusClient;
        private readonly IDataPublisher dataPublisher;

        private CancellationTokenSource? cts;

        public ModbusEngineCore(IModbusClient modbusClient, IDataPublisher dataPublisher)
        {
            this.modbusClient = modbusClient;
            this.dataPublisher = dataPublisher;
        }

        public Task StartAsync(ModbusDeviceConfig config)
        {
            cts = new CancellationTokenSource();
            _ = Task.Run(() => RunLoop(config, cts.Token));
            return Task.CompletedTask;
        }

        public void Stop()
        {
            cts?.Cancel();
        }

        private async Task RunLoop(ModbusDeviceConfig config, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var snapshot = ReadDevice(config);

                    dataPublisher.Publish(snapshot);
                }
                catch
                {
                    // swallow or route to error pipeline later
                }

                await Task.Delay(config.ScanRateMs, ct);
            }
        }

        // Read Coils/Input Status
        private List<string> ReadDigital(IModbusMaster mM, bool isInputs)
        {
            List<bool> rawData = new List<bool>();

            ushort start = Convert.ToUInt16(startAddress);

            // "Select" is a LINQ based method on C# array tro transform the elements to a new form.
            // Select transforms the data in a simple manner, but it does not return the set of the data implictly, therefore the results either get converted to an array, or they get spanned into an List via the syntax below.
            // [.. set.Select(...)] is unique and relatively new C# syntactic sugar for spanning your Select result onto an List.
            // ".." is the spread operator, meaning "Insert all elements from this sequence into the left side of the equal sign".
            // This is different from just an = sign, because we aren't assigning nD to equal this set, we are ADDING each of these elements iteratively to this set!
            // "(...)" is the collection expression, which contains the a lambda expression:
            // The lambda expression defines the transformation applied to each element in a sequence, used as a part of LINQ
            // x is the input parameter (one element of coils at a time)
            // => is the lambda operator in C#, effectively meaning "maps to"
            // Convert.ToInt16(x).ToString()) is the output expression, describing what needs to be done to x.
            // Think of lambda expressions like F(x) = x functions, where the syntax is instead: x => f(x)!
            // Note that the collection expression doesn't need to specify a data type (List<string>), because it already interprets this from the datatype of the object on the lefthand side of the equal sign, List<string> nD.

            // retrive the raw data from either of these NModbus calls, depending on the received bool
            var source = isInputs
                ? mM.ReadInputs(deviceId, start, dataLength)
                : mM.ReadCoils(deviceId, start, dataLength);

            // LINQ statement with the received raw data, now as a List<bool> instead of a bool[]
            rawData = [.. source];

            // If the returned data is not what we expect, report an error
            if (rawData == null || rawData.Count != dataLength)
            {
                throw new Exception("Received null or inadequate response when polling digital data.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                SuccessResp();
            }

            // Use another LINQ statement to convert the collection of bools into 1s and 0s
            return [.. rawData.Select(x => Convert.ToByte(x).ToString())];
        }

        // Read Holding Registers attempt
        private List<string> ReadHoldingRegs(IModbusMaster mM)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(startAddress);
            ushort[] holdingRegs = mM.ReadHoldingRegisters(deviceId, startAdd, dataLength);

            // If the returned data is not what we expect, report an error
            if (holdingRegs == null || holdingRegs.Length != dataLength)
            {
                throw new Exception("Received null or inadequate response for holding registers.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                SuccessResp();
            }

            // Convert registers to a parsed Observablecollection of strings using several helper methods
            // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
            return InterpretModbusRegs(holdingRegs);
        }

        // Read Input Registers attempt
        private List<string> ReadInputRegs(IModbusMaster mM)
        {
            // Request data over TCP
            ushort startAdd = Convert.ToUInt16(startAddress);
            ushort[] inputRegs = mM.ReadInputRegisters(deviceId, startAdd, dataLength);

            // If the returned data is not what we expect, report an error
            if (inputRegs == null || inputRegs.Length != dataLength)
            {
                throw new Exception("Received null or inadequate response for input registers.");
            }
            else
            {
                // Report a successful TCP response, now that we have the data
                SuccessResp();
            }

            // Convert registers to a parsed collection of strings using helper and update UI
            // This helper will handle endian transformation, numeric base formatting, and ASCII interpretation based on user settings.
            return InterpretModbusRegs(inputRegs);
        }

        // Helper Methods
        private List<string> InterpretModbusRegs(ushort[] receivedRegisters)
        {
            // Convert raw ushort registers into List<string> for UI display, applying user-selected transformations for data size, numeric base, endianness, and ASCII interpretation.
            var result = new List<string>();

            // Determine how many 16-bit registers compose one displayed value
            int regsPerValue = selectedDataSize switch
            {
                "32-Bit" => 2,
                "64-Bit" => 4,
                _ => 1, // "16-Bit" or default
            };

            // Calculate total bit width for formatting purposes (16, 32, 64)
            int bitWidth = regsPerValue * 16;

            // Loop through the registers in chunks corresponding to the selected data size (1 register for 16-bit, 2 for 32-bit, 4 for 64-bit)
            for (int i = 0; i < dataLength; i += regsPerValue)
            {
                if (i + regsPerValue - 1 >= receivedRegisters.Length)
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
                    ushort reg = receivedRegisters[i + j];

                    // Add high byte then low byte for each register to get original ordering from each register (MSB/Big Endian)
                    bytes.Add((byte)(reg >> 8));
                    bytes.Add((byte)(reg & 0xFF));
                }

                // Apply endian transformation to the series of bytes acquired, based on the selected endian option.
                ApplyEndianTransformation(bytes);

                // Format value according to data size, numeric base, and ASCII enable selection (Hex only)
                string formatted = FormatBytes(bytes.ToArray(), bitWidth, selectedNumericBase, asciiEnable);

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

        private void ApplyEndianTransformation(List<byte> bytes)
        {
            // bytes currently MSB-first per register: [reg0hi, reg0lo, reg1hi, reg1lo, ...]
            // Handle selected endian options.
            switch (selectedEndian)
            {
                case "Little Endian":
                    // Reverse full array: [a,b,c,d] -> [d,c,b,a]
                    bytes.Reverse();
                    break;
                case "Big Endian (Byte-Swap)":
                    // Swap bytes within each 16-bit word: [a,b,c,d] -> [b,a,d,c]
                    SwapBytesWithinWords(bytes);
                    break;
                case "Little Endian (Byte-Swap)":
                    // Reverse full array then swap within each word: [a,b,c,d] -> [d,c,b,a] -> [c,d,a,b]
                    bytes.Reverse();
                    SwapBytesWithinWords(bytes);
                    break;
                default:
                    // "Big Endian" -> keep as-is
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

        private string FormatBytes(byte[] bytes, int bitWidth, string numericBase, bool asciiEnable)
        {
            // Interpret bytes as MSB-first when manually constructing integers.
            // For floating point we need little-endian byte[] for BitConverter on typical platforms,
            // so reverse when calling BitConverter for floats/doubles.
            switch (numericBase)
            {
                case "Integer":
                    var le = bytes.Reverse().ToArray(); // BitConverter expects little-endian on typical platforms

                    // Use BitConverter to convert byte array to the appropriate integer type based on bit width, then convert to string for display.
                    return bitWidth switch
                    {
                        32 => BitConverter.ToInt32(le, 0).ToString(),
                        64 => BitConverter.ToInt64(le, 0).ToString(),
                        _ => BitConverter.ToInt16(le, 0).ToString(), // "16-Bit" or default
                    };

                case "Hexadecimal":
                    {
                        // Convert byte array to an unsigned long for hex formatting, since hex is typically used for raw values regardless of signedness.
                        ulong unsigned = ToUnsigned(bytes);
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

                case "Binary":
                    {
                        // Convert byte array to an unsigned long for binary formatting, since binary is typically used for raw values regardless of signedness.
                        ulong unsigned = ToUnsigned(bytes);

                        // Format binary with leading zeros based on bit width, and add spaces every 4 bits for readability.
                        string bin = Convert.ToString((long)unsigned, 2).PadLeft(bitWidth, '0');
                        string spaced = Regex.Replace(bin, ".{4}", "$0 ").Trim();
                        return spaced;
                    }

                case "Floating Point":
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
                    return ToUnsigned(bytes).ToString();
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
