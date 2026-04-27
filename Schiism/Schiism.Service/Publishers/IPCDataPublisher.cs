// <copyright file="IPCPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Publishers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Snapshots;

    // NOTE! These classes should be implemented in the .Service project! The interfaces belong here, but not the classes!
    // Move Publishers folder to .Service, when ready...

    // Dev/test output with an IPC (most likely "NamedPipes")
    public class IPCDataPublisher : IDataPublisher
    {
        public void PublishData(DataSnapshotDto snapshot)
        {
            // Console.WriteLine("Publish hit");
            // Console.WriteLine($"Device {snapshot.DeviceId} @ {snapshot.TimestampUtc} (UTC)");

            // foreach (var v in snapshot.Data)
            // {
            //     Console.Write($"{v} ");
            // }

            // Console.WriteLine();
        }
    }
}
