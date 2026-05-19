// <copyright file="FrontendInitState.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Implementations.IPC
{
    using Schiism.Core.Abstractions.IPC.States;

    /// <inheritdoc/>
    public class InitializedState : IInitializedState
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
