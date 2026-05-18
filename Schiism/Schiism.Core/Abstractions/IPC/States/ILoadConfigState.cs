// <copyright file="IFrontendInitState.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.States
{
    /// <summary>
    /// Interface for tracking initialization state of the frontend.
    /// </summary>
    public interface ILoadConfigState
    {
        /// <summary>
        /// Gets or sets a value indicating whether the frontend is initialized, whether that's a console or the WPF GUI.
        /// The Frontend is considered "Initialized" if it has booted up and has received the initial configuration from the Service.
        /// This is important for the Service to know, in case it needs to re-initialize a frontend that has been turned off, then booted up again.
        /// </summary>
        bool IsInitialized { get; set; }
    }
}
