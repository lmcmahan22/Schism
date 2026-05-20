// <copyright file="FileLoggerProvider.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Schiism.Service.FileLogging;
using System.IO;

namespace Schiism.WPF.FileLogging
{
    /// <summary>
    /// Creates the Logger and Directory for the Log File.
    /// </summary>
    public class WPFFileLoggerProvider : ILoggerProvider
    {
        private readonly string basePath;
        private readonly string fileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="WPFFileLoggerProvider"/> class.
        /// </summary>
        /// <param name="basePath">
        /// Base file path provided by Program.cs.
        /// </param>
        public WPFFileLoggerProvider(string basePath)
        {
            this.basePath = basePath;
            this.fileName = $"schiismWPF@{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.log";
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            string fullPath = Path.Combine(this.basePath, this.fileName);
            Directory.CreateDirectory(this.basePath); // Idempotent, meaning that if this already exists, nothing bad happens :D
            return new WPFFileLogger(fullPath);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
