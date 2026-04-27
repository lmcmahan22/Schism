// <copyright file="IModbusClient.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NModbus;
    using Schiism.Core.Models.Handlers;
    using Schiism.Core.Models.Enums;

    // "Contract for reading data." Simply define the methods and properties that can be used, since these will have unique logic between the different client classes.
    public interface IModbusClient
    {
        // Methods for reading MODBUS data from NMODBUS (eventually combine the two register methods into one, like digitals)!
        List<ushort> ReadData(string ip, int port, byte deviceId, ushort start, ushort length, int timeout, PollType dataType);
    }
}