// <copyright file="DataSnapshotDto.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.DTOs
{
    using System.Collections.Generic;

    /// <summary>
    /// Snapshot object contains Device ID and Data for streamlined Logging output.
    /// </summary>

    public class DataSnapshotDto
    {
        /// <summary>
        /// Gets or Sets Device ID.
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or Sets MODBUS TCP Data.
        /// </summary>
        public List<string> Data { get; set; } = new();
    }
}
