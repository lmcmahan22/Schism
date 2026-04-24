// <copyright file="FakeModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Enums;

    // TCP + MODBUS communication logic (interpretation of raw data is in the interpreter class, not here)
    // This should be different from the real MODBUSClient implementation class somehow? ChatGPT insists that this will be important for testing.
    public class FakeModbusClient : IModbusClient
    {
        private readonly Random rand = new();

        public List<ushort> ReadData(string ip, int port, byte deviceId, ushort start, ushort length, int timeout, PollType dataType)
        {
            // Implement later
            // return Enumerable.Range(0, length)
            //    .Select(_ => (ushort)rand.Next(0, 1000))
            //    .ToArray();
            return new List<ushort>();
        }
    }
}