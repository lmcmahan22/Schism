// <copyright file="ICommandReceiver.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.Commands
{
    using System.Threading.Tasks;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// Interface for the Command Receiver object.
    /// </summary>
    public interface ICommandReceiver
    {
        /// <summary>
        /// Asynchronous receive method.
        /// </summary>
        /// <param name="handler"> Handler is passed in, since the different projects that use the command receiver will use the command in different ways. Always of type SettingsConfig.</param>
        /// <param name="ct"> The cancellation token.</param>
        /// <returns>The asynchronous task.</returns>
        Task ReceiveAsync(Func<SettingsConfig, Task> handler, CancellationToken ct);
    }
}
