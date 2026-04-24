// <copyright file="IEnginePublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    using Schiism.Core.Models.Snapshots;

    // "Output Boundary"
    // Used for interacting with the console and service projects, once those are developed. Come back to this one later.
    public interface IEngineDiagnostics
    {
        void PublishError(ErrorSnapshotDto snapshot);
    }
}
