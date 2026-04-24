// <copyright file="ConsolePublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Publishers
{
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Snapshots;

    // Dev/test output with a console
    public class ConsolePublisher : IDataPublisher, IEngineDiagnostics
    {

        public void PublishData(DataSnapshotDto snapshot)
        {
            Console.WriteLine("Publish hit");
            Console.WriteLine($"Device {snapshot.DeviceId} @ {snapshot.TimestampUtc}");

            foreach (var v in snapshot.Data)
            {
                Console.Write($"{v} ");
            }

            Console.WriteLine();
        }

        public void PublishError(ErrorSnapshotDto snapshot)
        {
            Console.WriteLine("Error hit");
            Console.WriteLine($"Device {snapshot.DeviceId} @ {snapshot.TimestampUtc}");
            Console.WriteLine("Error:", snapshot.Error);
        }
    }
}
