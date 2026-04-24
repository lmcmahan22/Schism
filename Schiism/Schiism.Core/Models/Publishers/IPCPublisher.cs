// <copyright file="IPCPublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Models.Publishers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Schiism.Core.Abstractions;
    using Schiism.Core.Models.Snapshot;

    // Dev/test output with an IPC (most likely "NamedPipes")
    public class IPCPublisher : IEnginePublisher
    {
        public void Publish(DataSnapshotDto snapshot)
        {
            // Define later
        }
    }
}
