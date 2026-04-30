// <copyright file="IEnginePublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core.Abstractions
{
    /// <summary>
    /// Interface for publishing Engine information (i.e Info, Warnings, and Errors).
    /// </summary>
    public interface IEnginePublisher
    {
        /// <summary>
        /// Return provided message as an Information log line.
        /// </summary>
        /// <param name="message">
        /// Message string.
        /// </param>
        void Info(string message);

        /// <summary>
        /// Return provided message as an Warning log line.
        /// </summary>
        /// <param name="message">
        /// Message string.
        /// </param>
        void Warning(string message);

        /// <summary>
        /// Return provided message as an Error log line (highlighted red in powershell console).
        /// </summary>
        /// <param name="message">
        /// Message string.
        /// </param>
        /// <param name="ex">
        /// Caught exception.
        /// </param>
        void Error(string message, Exception ex);
    }
}
