// <copyright file="ICommandSender.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions.IPC.Commands
{
    using System.Threading.Tasks;
    using Schiism.Core.Models.IPC.DTOs.Commands;

    /// <summary>
    /// ICommandSender interface used for defining command send implementations on the frontend and backend projects.
    /// </summary>
    public interface ICommandSender
    {
        /// <summary>
        /// Ayncrhonous sending method. No looping here, only a singular send.
        /// </summary>
        /// <param name="config">Configuration settings to be sent on the command.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendAsync(SettingsConfig config, CancellationToken ct);
    }
}
