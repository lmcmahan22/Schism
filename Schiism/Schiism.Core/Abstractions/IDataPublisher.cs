// <copyright file="IDataPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Snapshots;

    /// <summary>
    /// Interface for publishing MODBUS data, one publish per poll.
    /// </summary>
    public interface IDataPublisher
    {
        /// <summary>
        /// Method accepts a snapshot object as an input parameter.
        /// </summary>
        /// <param name="snapshot">
        /// Contains the device ID and the MODBUS data for that particular poll.
        /// </param>
        void PublishData(DataSnapshotDto snapshot);
    }
}
