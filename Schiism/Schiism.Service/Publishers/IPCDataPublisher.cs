// <copyright file="IPCDataPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Publishers
{
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Snapshots;

    /// <inheritdoc/>
    public class IPCDataPublisher(ILogger<IPCDataPublisher> logger) : IDataPublisher
    {
        /// <inheritdoc/>
        public void PublishData(DataSnapshotDto snapshot)
        {
            logger.LogInformation(
                "Device {DeviceId} | Data: {Data}",
                snapshot.DeviceId,
                string.Join(", ", snapshot.Data)
            );
        }
    }
}
