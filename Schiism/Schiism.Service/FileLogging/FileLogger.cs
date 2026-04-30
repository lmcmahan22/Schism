// <copyright file="FileLogger.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Service.FileLogging
{
    /// <summary>
    /// ILogger implementation to send Logged messages to a timestamped text file.
    /// NOTE: A lock is implemented since the WorkerService may delegate error logging and data logging to different threads.
    /// </summary>
    public class FileLogger : ILogger
    {
        private static readonly object TextAppendlock = new();
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogger"/> class.
        /// </summary>
        /// <param name="filePath">
        /// File path received from the Program.cs logic, where the file directory can be modified.
        /// </param>
        public FileLogger(string filePath)
        {
            this.filePath = filePath;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => null;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string message = $"{eventId} {timestamp} [{logLevel}] {formatter(state, exception)}";

            lock (TextAppendlock)
            {
                File.AppendAllText(this.filePath, message + Environment.NewLine);
            }
        }
    }
}
