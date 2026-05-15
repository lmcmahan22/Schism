// <copyright file="FrontendInitState.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using Schiism.Core.Abstractions.IPC;

    /// <inheritdoc/>
    public class FrontendInitState : IFrontendInitState
    {
        private volatile bool isInitialized;

        /// <inheritdoc/>
        public bool IsInitialized
        {
            get => this.isInitialized;
            set => this.isInitialized = value;
        }
    }
}
