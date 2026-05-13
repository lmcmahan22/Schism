// <copyright file="FileLoggerProvider.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.FileLogging
{
    /// <summary>
    /// Creates the Logger and Directory for the Log File.
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string basePath;
        private readonly string fileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
        /// </summary>
        /// <param name="basePath">
        /// Base file path provided by Program.cs.
        /// </param>
        public FileLoggerProvider(string basePath)
        {
            this.basePath = basePath;
            fileName = $"schiism@{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.log";
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            var fullPath = Path.Combine(basePath, fileName);
            Directory.CreateDirectory(basePath); // Idempotent, meaning that if this already exists, nothing bad happens :D
            return new FileLogger(fullPath);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
