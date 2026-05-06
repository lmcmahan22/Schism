// <copyright file="EnginePublisher.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.Models.Implementations.Publishers
{
    using System;
    using Schiism.Core.Abstractions.Publishers;

    /// <inheritdoc/>
    public class EnginePublisher(ILogger<EnginePublisher> logger) : IEnginePublisher
    {
        /// <inheritdoc/>
        public void Info(string message)
        {
            logger.LogInformation(message);
        }

        /// <inheritdoc/>
        public void Warning(string message)
        {
            logger.LogWarning(message);
        }

        /// <inheritdoc/>
        public void Error(string message, Exception ex)
        {
            logger.LogError(message, ex);
        }
    }
}
