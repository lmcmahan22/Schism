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
        // This refers to the singleton defined in the Program.cs file, managed by the DI container!
        private readonly ILogger<IPCDataPublisher> _logger;

        public IPCDataPublisher(ILogger<IPCDataPublisher> logger)
        {
            _logger = logger;
        }

        public void PublishData(DataSnapshotDto snapshot)
        {
            _logger.LogInformation($"Device {snapshot.DeviceId} @ {snapshot.TimestampUtc} (UTC)");

            foreach (var v in snapshot.Data)
            {
                _logger.LogInformation($"{v} ");
            }

            Console.WriteLine();
        }
    }
}
