// <copyright file="DataSnapshotDto.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Snapshots
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    // A point-in-time snapshot of a device’s data, formatted and ready to leave Core.
    // Device → which Modbus unit this came from
    // Snapshot → captured at a specific moment in time
    // DTO → safe, simple structure for transport (IPC, logging, UI, etc.)
    public class DataSnapshotDto
    {
        public int DeviceId { get; set; }

        public List<string> Data { get; set; } = new();
    }
}
