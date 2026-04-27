namespace Schiism.Service.Publishers
{
    using System;
    using Schiism.Core.Abstractions;

    public class EngineLogger : IEngineLogger
    {
        // Create the Windows Logger for this class
        private readonly ILogger<EngineLogger> _logger;

        public EngineLogger(ILogger<EngineLogger> logger)
        {
            _logger = logger;
        }

        // Implement in Engine if you need to observe/timestamp when expected events occur. Not incredibly necessary unless the you or the user need more transparency.
        public void Info(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        // Similar to Error, but should execute if the system is still running (i.e. program didn't need to leave the try loop)
        public void Warning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void Error(string message, Exception ex, params object[] args)
        {
            _logger.LogError(message, ex, args);
        }
    }
}
