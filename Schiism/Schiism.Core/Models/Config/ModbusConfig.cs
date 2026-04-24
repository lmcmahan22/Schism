// <copyright file="ModbusConfig.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Config
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Schiism.Core.Models.Enums;

    // Used by the engine as a collection of necessary request parameters
    public class ModbusConfig
    {
        // Private wariables
        private string ipAddress;
        private byte dataLength;
        private ushort startAddress;
        private DataSize selectedDataSize;

        public ModbusConfig()
        {
            this.ipAddress = "100.100.100.100";
            this.dataLength = 10;
            this.startAddress = 0;
            this.selectedDataSize = DataSize.Bit16;
        }

        // Properties for connection settings
        public string IPAddress
        {
            get => this.ipAddress;
            set => this.ipAddress = value;
        }

        public int TCPPort { get; set; }

        public int ScanRate { get; set; }

        public int TCPTimeout { get; set; }

        public byte DeviceId { get; set; }

        public byte DataLength
        {
            get => this.dataLength;
            set
            {
                // Take the currently set Data Size into account
                byte minLen = this.GetMinLengthForStartAddress();

                // Take the currently set StartingAddress into account
                byte maxLen = this.GetMaxLengthForStartAddress();

                // We already verified that the value is within our desired boundaries, so we simply need to check for a difference.
                if (this.dataLength != value)
                {
                    this.dataLength = value;
                }
            }
        }

        public ushort StartAddress
        {
            get => this.startAddress;
            set
            {
                // temp variable to help store the incoming decimal value, after possible hex conversion
                uint attemptDecVal = 0;

                // StartAddress changed to ushort, because this string handling should be managed netirely in the UI
                // If the value contains "h"
                // if (value.Contains('h'))
                // {
                //    // Get rid of the "h" at the end ex. "Ah -> A"
                //    string trun = value.Substring(0, value.Length - 1);

                // convert hex string into a decimal int ex. "A -> 10"
                //    attemptDecVal = Convert.ToUInt32(trun, 16);
                // }

                // If the value contains just numbers (no "h")
                // else
                // {
                //    attemptDecVal = Convert.ToUInt32(value);
                // }

                // We can now confirm that the attempted decimal converted value is a short (1-65535), so we can type cast it!
                ushort decVal = Convert.ToUInt16(attemptDecVal);

                // Update approved value onto the startAddress string
                if (this.startAddress != decVal)
                {
                    this.startAddress = decVal;

                    // Adjust length to this newly accepted startAddress, in the event that our original length is no longer compatible
                    byte maxLen = this.GetMaxLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(this.dataLength, (byte)1, maxLen);

                    if (this.dataLength != clampedDataLength)
                    {
                        this.dataLength = clampedDataLength;
                    }
                }
            }
        }

        public PollType SelectedPollType { get; set; }

        // Properties for Modbus Data interpretation
        public bool AsciiEnable { get; set; }

        public DataSize SelectedDataSize
        {
            get => this.selectedDataSize;
            set
            {
                if (this.selectedDataSize != value)
                {
                    this.selectedDataSize = value;

                    // Adjust length to this newly accepted startAddress, in the event that our original length is no longer compatible
                    byte minLen = this.GetMinLengthForStartAddress();
                    byte clampedDataLength = Math.Clamp(this.dataLength, minLen, (byte)120);

                    if (this.dataLength != clampedDataLength)
                    {
                        this.dataLength = clampedDataLength;
                    }
                }
            }
        }

        public NumericBase SelectedNumericBase { get; set; }

        public Endian SelectedEndian { get; set; }

        // Prevent user from prompting a data overflow simply due to configuring the length and data size poorly
        private byte GetMinLengthForStartAddress()
        {
            return this.selectedDataSize switch
            {
                DataSize.Bit32 => 2,
                DataSize.Bit64 => 4,
                _ => 1, // "Bit16" or default
            };
        }

        // Prevent user from prompting a data overflow simply due to configuring the length and starting address poorly
        private byte GetMaxLengthForStartAddress()
        {
            int cap = ushort.MaxValue - this.startAddress + 1; // inclusive cap (stroed as an int, because this could be 65536 in the event that the StartingAddress is curently 0. If so, that is okay, because 120 will end up being the minimum.
            ushort clamped = (ushort)Math.Min(120, cap);
            return (byte)clamped;
        }
    }
}
