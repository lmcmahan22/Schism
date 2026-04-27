// <copyright file="ConsolePublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Publishers
{
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Snapshots;

    // Dev/test output with a console
    public class ConsoleDataPublisher : IDataPublisher
    {
        public void PublishData(DataSnapshotDto snapshot)
        {
            Console.WriteLine("Publish hit");
            Console.WriteLine($"Device {snapshot.DeviceId} @ {snapshot.TimestampUtc} (UTC)");

            foreach (var v in snapshot.Data)
            {
                Console.Write($"{v} ");
            }

            Console.WriteLine();
        }
    }
}
