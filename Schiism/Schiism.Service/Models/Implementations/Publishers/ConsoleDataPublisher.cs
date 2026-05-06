// <copyright file="ConsoleDataPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations.Publishers
{
    using Schiism.Core.Abstractions.Logging;
    using Schiism.Core.Models.DTOs;

    /// <inheritdoc/>
    public class ConsoleDataPublisher : IDataPublisher
    {
        /// <inheritdoc/>
        public void PublishData(DataSnapshotDto snapshot)
        {
            Console.WriteLine("Publish hit");
            Console.WriteLine($"Device {snapshot.DeviceId}");

            foreach (var v in snapshot.Data)
            {
                Console.Write($"{v} ");
            }

            Console.WriteLine();
        }
    }
}
